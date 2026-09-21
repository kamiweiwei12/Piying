using System;
using System.Collections.Generic;

namespace YingYun.Rhythm.Judgment
{
    /// <summary>
    /// 可重現的純 C# 判定核心。呼叫端提供 song time 與帶時間戳的輸入；本類不依賴 Unity API。
    /// </summary>
    public sealed class JudgmentEngine
    {
        private const double TimeEpsilon = 1e-9d;

        private sealed class NoteState
        {
            public NoteState(NoteData data)
            {
                Data = data;
            }

            public NoteData Data { get; }
            public bool IsHolding { get; set; }
            public bool IsJudged { get; set; }
            public JudgmentGrade StartGrade { get; set; }
            public double StartErrorMs { get; set; }
            public double HoldPressTimeSec { get; set; }
            public double NextTickTimeSec { get; set; }
        }

        private sealed class QueuedInput
        {
            public QueuedInput(HitInput value, long sequence)
            {
                Value = value;
                Sequence = sequence;
            }

            public HitInput Value { get; }
            public long Sequence { get; }
        }

        private sealed class SegmentState
        {
            public int Total { get; set; }
            public int Judged { get; set; }
            public bool HasMiss { get; set; }
            public bool Reported { get; set; }
        }

        private readonly TimingConfig _config;
        private readonly ISongClock _clock;
        private readonly List<NoteState> _notes;
        private readonly List<QueuedInput> _inputs = new List<QueuedInput>();
        private readonly List<int> _candidateInputIndices = new List<int>(6);
        private readonly List<int> _setInputIndices = new List<int>(6);
        private readonly List<int> _bestInputIndices = new List<int>(6);
        private readonly Dictionary<int, SegmentState> _segments = new Dictionary<int, SegmentState>();
        private List<JudgmentResult> _results;
        private long _nextInputSequence;
        private double _accuracyWeight;

        public JudgmentEngine(IEnumerable<NoteData> notes, TimingConfig config, ISongClock clock)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            _config = config;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _notes = new List<NoteState>();
            var ids = new HashSet<int>();

            foreach (NoteData note in notes)
            {
                if (!ids.Add(note.Id))
                {
                    throw new ArgumentException("Note ids must be unique.", nameof(notes));
                }

                if (note.Kind == NoteKind.Hold && note.IsChord)
                {
                    throw new NotSupportedException("Chord holds are not supported by the M5 prototype.");
                }

                _notes.Add(new NoteState(note));
                if (!_segments.TryGetValue(note.SegmentId, out SegmentState segment))
                {
                    segment = new SegmentState();
                    _segments.Add(note.SegmentId, segment);
                }

                segment.Total++;
            }

            _notes.Sort((a, b) =>
            {
                int timeComparison = a.Data.TimeSec.CompareTo(b.Data.TimeSec);
                return timeComparison != 0 ? timeComparison : a.Data.Id.CompareTo(b.Data.Id);
            });
        }

        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Score { get; private set; }
        public int JudgedNoteCount { get; private set; }
        public int PendingInputCount => _inputs.Count;
        public double Accuracy => JudgedNoteCount == 0 ? 0d : _accuracyWeight / JudgedNoteCount;

        public void EnqueueInput(HitInput input)
        {
            _inputs.Add(new QueuedInput(input, _nextInputSequence++));
        }

        /// <summary>
        /// 處理目前 song time 的輸入、Hold 與逾時，並將本次事件寫入呼叫端可重用的清單以避免熱路徑配置。
        /// </summary>
        public IReadOnlyList<JudgmentResult> Advance(List<JudgmentResult> output)
        {
            _results = output ?? throw new ArgumentNullException(nameof(output));
            double songTimeSec = _clock.SongTime;
            _results.Clear();
            MatchPressInputs(songTimeSec);
            ProcessHolds(songTimeSec);
            DiscardOrphanReleases(songTimeSec);
            ProcessMisses(songTimeSec);
            return output;
        }

        private void MatchPressInputs(double songTimeSec)
        {
            while (true)
            {
                int bestNoteIndex = -1;
                double bestError = double.MaxValue;
                long bestSequence = long.MaxValue;
                double bestSignedErrorSec = 0d;
                _bestInputIndices.Clear();

                for (int noteIndex = 0; noteIndex < _notes.Count; noteIndex++)
                {
                    NoteState note = _notes[noteIndex];
                    if (note.IsJudged || note.IsHolding)
                    {
                        continue;
                    }

                    if (!TryBuildInputSet(
                            note,
                            songTimeSec,
                            _candidateInputIndices,
                            out double signedErrorSec,
                            out long firstSequence))
                    {
                        continue;
                    }

                    double error = Math.Abs(signedErrorSec);
                    if (error < bestError || (error.Equals(bestError) && firstSequence < bestSequence))
                    {
                        bestNoteIndex = noteIndex;
                        bestError = error;
                        bestSequence = firstSequence;
                        bestSignedErrorSec = signedErrorSec;
                        _bestInputIndices.Clear();
                        _bestInputIndices.AddRange(_candidateInputIndices);
                    }
                }

                if (bestNoteIndex < 0)
                {
                    return;
                }

                NoteState matchedNote = _notes[bestNoteIndex];
                _bestInputIndices.Sort();
                for (int i = _bestInputIndices.Count - 1; i >= 0; i--)
                {
                    _inputs.RemoveAt(_bestInputIndices[i]);
                }

                JudgmentGrade grade = GradeForError(bestSignedErrorSec);

                if (matchedNote.Data.Kind == NoteKind.Hold)
                {
                    matchedNote.IsHolding = true;
                    matchedNote.StartGrade = grade;
                    matchedNote.StartErrorMs = bestSignedErrorSec * 1000d;
                    matchedNote.HoldPressTimeSec = matchedNote.Data.TimeSec + bestSignedErrorSec;
                    matchedNote.NextTickTimeSec = matchedNote.Data.TimeSec + _config.HoldTickIntervalSec;
                    AddResult(
                        JudgmentEventKind.HoldStarted,
                        matchedNote.Data,
                        grade,
                        bestSignedErrorSec * 1000d);
                }
                else
                {
                    CompleteNote(matchedNote, grade, bestSignedErrorSec * 1000d);
                }
            }
        }

        private bool TryBuildInputSet(
            NoteState note,
            double songTimeSec,
            List<int> matchedIndices,
            out double signedWorstErrorSec,
            out long firstSequence)
        {
            matchedIndices.Clear();
            signedWorstErrorSec = 0d;
            firstSequence = long.MaxValue;
            double bestAbsoluteError = double.MaxValue;

            for (int anchorIndex = 0; anchorIndex < _inputs.Count; anchorIndex++)
            {
                HitInput anchor = _inputs[anchorIndex].Value;
                if (anchor.Kind != InputKind.Press ||
                    anchor.InputTimeSec > songTimeSec ||
                    (note.Data.RequiredLanesMask & (1 << anchor.Lane)) == 0 ||
                    Math.Abs(anchor.InputTimeSec - note.Data.TimeSec) > _config.GoodWindowSec + TimeEpsilon)
                {
                    continue;
                }

                _setInputIndices.Clear();
                double candidateSignedWorstError = 0d;
                long candidateFirstSequence = long.MaxValue;
                bool complete = true;

                for (int lane = 0; lane < 31; lane++)
                {
                    if ((note.Data.RequiredLanesMask & (1 << lane)) == 0)
                    {
                        continue;
                    }

                    int closestIndex = -1;
                    double closestAbsoluteError = double.MaxValue;
                    long closestSequence = long.MaxValue;
                    for (int inputIndex = 0; inputIndex < _inputs.Count; inputIndex++)
                    {
                        QueuedInput queued = _inputs[inputIndex];
                        HitInput input = queued.Value;
                        if (input.Kind != InputKind.Press ||
                            input.InputTimeSec > songTimeSec ||
                            input.Lane != lane ||
                            input.InputTimeSec < anchor.InputTimeSec - TimeEpsilon ||
                            input.InputTimeSec > anchor.InputTimeSec + _config.ChordSpreadWindowSec + TimeEpsilon)
                        {
                            continue;
                        }

                        double absoluteError = Math.Abs(input.InputTimeSec - note.Data.TimeSec);
                        if (absoluteError > _config.GoodWindowSec + TimeEpsilon)
                        {
                            continue;
                        }

                        if (absoluteError < closestAbsoluteError ||
                            (absoluteError.Equals(closestAbsoluteError) && queued.Sequence < closestSequence))
                        {
                            closestIndex = inputIndex;
                            closestAbsoluteError = absoluteError;
                            closestSequence = queued.Sequence;
                        }
                    }

                    if (closestIndex < 0)
                    {
                        complete = false;
                        break;
                    }

                    QueuedInput closest = _inputs[closestIndex];
                    _setInputIndices.Add(closestIndex);
                    candidateFirstSequence = Math.Min(candidateFirstSequence, closest.Sequence);
                    double signedError = closest.Value.InputTimeSec - note.Data.TimeSec;
                    if (Math.Abs(signedError) > Math.Abs(candidateSignedWorstError))
                    {
                        candidateSignedWorstError = signedError;
                    }
                }

                double candidateAbsoluteError = Math.Abs(candidateSignedWorstError);
                if (!complete ||
                    (candidateAbsoluteError > bestAbsoluteError && !candidateAbsoluteError.Equals(bestAbsoluteError)) ||
                    (candidateAbsoluteError.Equals(bestAbsoluteError) && candidateFirstSequence >= firstSequence))
                {
                    continue;
                }

                matchedIndices.Clear();
                matchedIndices.AddRange(_setInputIndices);
                signedWorstErrorSec = candidateSignedWorstError;
                firstSequence = candidateFirstSequence;
                bestAbsoluteError = candidateAbsoluteError;
            }

            return matchedIndices.Count > 0;
        }

        private void ProcessHolds(double songTimeSec)
        {
            for (int noteIndex = 0; noteIndex < _notes.Count; noteIndex++)
            {
                NoteState note = _notes[noteIndex];
                if (!note.IsHolding || note.IsJudged)
                {
                    continue;
                }

                int releaseIndex = FindFirstRelease(note, songTimeSec);
                double heldUntil = releaseIndex >= 0 ? _inputs[releaseIndex].Value.InputTimeSec : songTimeSec;
                double tickLimit = Math.Min(heldUntil, note.Data.EndTimeSec);

                while (note.NextTickTimeSec <= tickLimit)
                {
                    Score += _config.HoldTickScore;
                    AddResult(JudgmentEventKind.HoldTick, note.Data, note.StartGrade, 0d);
                    note.NextTickTimeSec += _config.HoldTickIntervalSec;
                }

                if (releaseIndex >= 0)
                {
                    HitInput release = _inputs[releaseIndex].Value;
                    _inputs.RemoveAt(releaseIndex);
                    double releaseErrorSec = release.InputTimeSec - note.Data.EndTimeSec;
                    if (releaseErrorSec < -_config.GoodWindowSec - TimeEpsilon)
                    {
                        CompleteNote(note, JudgmentGrade.Miss, releaseErrorSec * 1000d);
                    }
                    else if (releaseErrorSec >= 0d)
                    {
                        // 玩家已撐過尾端；晚放不再要求卡在狹窄的尾判窗內。
                        CompleteNote(note, note.StartGrade, note.StartErrorMs);
                    }
                    else
                    {
                        JudgmentGrade releaseGrade = GradeForError(releaseErrorSec);
                        bool startIsWorse = GradeWeight(note.StartGrade) <= GradeWeight(releaseGrade);
                        CompleteNote(
                            note,
                            startIsWorse ? note.StartGrade : releaseGrade,
                            startIsWorse ? note.StartErrorMs : releaseErrorSec * 1000d);
                    }
                }
                else if (songTimeSec + TimeEpsilon >= note.Data.EndTimeSec)
                {
                    // 持續按住到尾端即完成，不強迫玩家在尾端 ±N ms 精準鬆手。
                    CompleteNote(note, note.StartGrade, note.StartErrorMs);
                }
            }
        }

        private int FindFirstRelease(NoteState note, double songTimeSec)
        {
            int bestIndex = -1;
            double bestTime = double.MaxValue;
            long bestSequence = long.MaxValue;

            for (int i = 0; i < _inputs.Count; i++)
            {
                QueuedInput queued = _inputs[i];
                HitInput input = queued.Value;
                if (input.Kind != InputKind.Release ||
                    input.Lane != note.Data.Lane ||
                    input.InputTimeSec < note.HoldPressTimeSec - TimeEpsilon ||
                    input.InputTimeSec > songTimeSec)
                {
                    continue;
                }

                if (input.InputTimeSec < bestTime || (input.InputTimeSec.Equals(bestTime) && queued.Sequence < bestSequence))
                {
                    bestIndex = i;
                    bestTime = input.InputTimeSec;
                    bestSequence = queued.Sequence;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Release 只可能屬於呼叫當下已開始的 Hold；ProcessHolds 未消耗的舊 Release
        /// 不得留到未來污染同軌長按。
        /// </summary>
        private void DiscardOrphanReleases(double songTimeSec)
        {
            for (int i = _inputs.Count - 1; i >= 0; i--)
            {
                HitInput input = _inputs[i].Value;
                if (input.Kind == InputKind.Release && input.InputTimeSec <= songTimeSec + TimeEpsilon)
                {
                    _inputs.RemoveAt(i);
                }
            }
        }

        private void ProcessMisses(double songTimeSec)
        {
            for (int i = 0; i < _notes.Count; i++)
            {
                NoteState note = _notes[i];
                if (!note.IsJudged && !note.IsHolding && songTimeSec > note.Data.TimeSec + _config.GoodWindowSec + TimeEpsilon)
                {
                    CompleteNote(note, JudgmentGrade.Miss, (songTimeSec - note.Data.TimeSec) * 1000d);
                }
            }
        }

        private JudgmentGrade GradeForError(double errorSec)
        {
            double absoluteError = Math.Abs(errorSec);
            if (absoluteError <= _config.PerfectWindowSec + TimeEpsilon)
            {
                return JudgmentGrade.Perfect;
            }

            return absoluteError <= _config.GreatWindowSec + TimeEpsilon
                ? JudgmentGrade.Great
                : JudgmentGrade.Good;
        }

        private void CompleteNote(NoteState note, JudgmentGrade grade, double errorMs)
        {
            note.IsHolding = false;
            note.IsJudged = true;
            JudgedNoteCount++;

            double weight;
            if (grade == JudgmentGrade.Miss)
            {
                Combo = 0;
                weight = 0d;
            }
            else
            {
                Combo++;
                MaxCombo = Math.Max(MaxCombo, Combo);
                weight = grade == JudgmentGrade.Perfect
                    ? 1d
                    : grade == JudgmentGrade.Great ? 0.75d : 0.5d;
                double comboMultiplier = 1d + Math.Min(Combo, _config.MaxComboBonus) / (double)_config.MaxComboBonus;
                Score += (int)Math.Round(_config.BaseScorePerNote * weight * comboMultiplier, MidpointRounding.AwayFromZero);
            }

            _accuracyWeight += weight;
            AddResult(JudgmentEventKind.NoteJudged, note.Data, grade, errorMs);

            SegmentState segment = _segments[note.Data.SegmentId];
            segment.Judged++;
            segment.HasMiss |= grade == JudgmentGrade.Miss;
            if (!segment.Reported && segment.HasMiss)
            {
                segment.Reported = true;
                AddResult(JudgmentEventKind.SegmentInterrupted, note.Data, JudgmentGrade.Miss, 0d);
            }
            else if (!segment.Reported && segment.Judged == segment.Total)
            {
                segment.Reported = true;
                AddResult(JudgmentEventKind.SegmentCompleted, note.Data, JudgmentGrade.None, 0d);
            }
        }

        private static double GradeWeight(JudgmentGrade grade)
        {
            switch (grade)
            {
                case JudgmentGrade.Perfect: return 1d;
                case JudgmentGrade.Great: return 0.75d;
                case JudgmentGrade.Good: return 0.5d;
                default: return 0d;
            }
        }

        private void AddResult(JudgmentEventKind eventKind, NoteData note, JudgmentGrade grade, double errorMs)
        {
            _results.Add(new JudgmentResult(
                eventKind,
                note.Id,
                note.SegmentId,
                grade,
                errorMs,
                Combo,
                Score,
                Accuracy,
                note.RequiredLanesMask));
        }
    }
}
