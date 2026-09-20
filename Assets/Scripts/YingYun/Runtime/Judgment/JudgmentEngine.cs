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
            ProcessMisses(songTimeSec);
            return output;
        }

        private void MatchPressInputs(double songTimeSec)
        {
            while (true)
            {
                int bestNoteIndex = -1;
                int bestInputIndex = -1;
                double bestError = double.MaxValue;
                long bestSequence = long.MaxValue;

                for (int noteIndex = 0; noteIndex < _notes.Count; noteIndex++)
                {
                    NoteState note = _notes[noteIndex];
                    if (note.IsJudged || note.IsHolding)
                    {
                        continue;
                    }

                    for (int inputIndex = 0; inputIndex < _inputs.Count; inputIndex++)
                    {
                        QueuedInput queued = _inputs[inputIndex];
                        HitInput input = queued.Value;
                        if (input.Kind != InputKind.Press || input.InputTimeSec > songTimeSec || input.Lane != note.Data.Lane)
                        {
                            continue;
                        }

                        double error = Math.Abs(input.InputTimeSec - note.Data.TimeSec);
                        if (error > _config.GoodWindowSec + TimeEpsilon)
                        {
                            continue;
                        }

                        if (error < bestError || (error.Equals(bestError) && queued.Sequence < bestSequence))
                        {
                            bestNoteIndex = noteIndex;
                            bestInputIndex = inputIndex;
                            bestError = error;
                            bestSequence = queued.Sequence;
                        }
                    }
                }

                if (bestNoteIndex < 0)
                {
                    return;
                }

                NoteState matchedNote = _notes[bestNoteIndex];
                HitInput matchedInput = _inputs[bestInputIndex].Value;
                _inputs.RemoveAt(bestInputIndex);
                double signedErrorSec = matchedInput.InputTimeSec - matchedNote.Data.TimeSec;
                JudgmentGrade grade = GradeForError(signedErrorSec);

                if (matchedNote.Data.Kind == NoteKind.Hold)
                {
                    matchedNote.IsHolding = true;
                    matchedNote.StartGrade = grade;
                    matchedNote.StartErrorMs = signedErrorSec * 1000d;
                    matchedNote.NextTickTimeSec = matchedNote.Data.TimeSec + _config.HoldTickIntervalSec;
                }
                else
                {
                    CompleteNote(matchedNote, grade, signedErrorSec * 1000d);
                }
            }
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

                int releaseIndex = FindFirstRelease(note.Data.Lane, songTimeSec);
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
                    else
                    {
                        CompleteNote(note, note.StartGrade, note.StartErrorMs);
                    }
                }
                else if (songTimeSec > note.Data.EndTimeSec + _config.GoodWindowSec + TimeEpsilon)
                {
                    CompleteNote(note, JudgmentGrade.Miss, (songTimeSec - note.Data.EndTimeSec) * 1000d);
                }
            }
        }

        private int FindFirstRelease(int lane, double songTimeSec)
        {
            int bestIndex = -1;
            double bestTime = double.MaxValue;
            long bestSequence = long.MaxValue;

            for (int i = 0; i < _inputs.Count; i++)
            {
                QueuedInput queued = _inputs[i];
                HitInput input = queued.Value;
                if (input.Kind != InputKind.Release || input.Lane != lane || input.InputTimeSec > songTimeSec)
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
            return absoluteError <= _config.PerfectWindowSec + TimeEpsilon ? JudgmentGrade.Perfect : JudgmentGrade.Good;
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
                weight = grade == JudgmentGrade.Perfect ? 1d : 0.5d;
                double comboMultiplier = 1d + Math.Min(Combo, _config.MaxComboBonus) / (double)_config.MaxComboBonus;
                Score += (int)Math.Round(_config.BaseScorePerNote * weight * comboMultiplier, MidpointRounding.AwayFromZero);
            }

            _accuracyWeight += weight;
            AddResult(JudgmentEventKind.NoteJudged, note.Data, grade, errorMs);

            SegmentState segment = _segments[note.Data.SegmentId];
            segment.Judged++;
            segment.HasMiss |= grade == JudgmentGrade.Miss;
            if (!segment.Reported && segment.Judged == segment.Total)
            {
                segment.Reported = true;
                AddResult(
                    segment.HasMiss ? JudgmentEventKind.SegmentInterrupted : JudgmentEventKind.SegmentCompleted,
                    note.Data,
                    segment.HasMiss ? JudgmentGrade.Miss : JudgmentGrade.None,
                    0d);
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
                Accuracy));
        }
    }
}
