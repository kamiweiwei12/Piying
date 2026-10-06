using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Chart
{
    /// <summary>
    /// 建立原型使用的八拍操偶舞句：六個部位都有點按，也有長按。
    /// 長按與點按由音符本身的長度決定（長度 &gt; 0 即為長按），所有時間均由 BPM 換算為秒。
    /// </summary>
    public static class PrototypeDanceChart
    {
        public const int LeftHandLane = 0;
        public const int HeadLane = 1;
        public const int RightHandLane = 2;
        public const int LeftFootLane = 3;
        public const int BodyLane = 4;
        public const int RightFootLane = 5;

        private const int BeatsPerPhrase = 8;

        /// <summary>長按長度（拍）：120 BPM 下等於 1 秒。</summary>
        private const double HoldBeats = 2d;

        public static NoteData[] Create(double bpm, double durationSeconds)
        {
            return Create(bpm, durationSeconds, PlayDifficulty.Normal);
        }

        public static NoteData[] Create(double bpm, double durationSeconds, PlayDifficulty difficulty)
        {
            if (bpm <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            if (durationSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            if (difficulty == PlayDifficulty.Easy)
            {
                return CreateEasy(durationSeconds);
            }

            return difficulty == PlayDifficulty.Hard
                ? CreateHard(bpm, durationSeconds)
                : CreateNormal(bpm, durationSeconds);
        }

        private static NoteData[] CreateNormal(double bpm, double durationSeconds)
        {
            double beatDuration = 60d / bpm;
            double phraseDuration = beatDuration * BeatsPerPhrase;
            int phraseCount = (int)Math.Floor(durationSeconds / phraseDuration);
            var notes = new List<NoteData>((phraseCount * 6) + 1);
            int noteId = 1;

            for (int phrase = 0; phrase < phraseCount; phrase++)
            {
                double start = phrase * phraseDuration;
                int laneBase = phrase % 6;
                notes.Add(Tap(noteId++, laneBase, start, phrase));
                notes.Add(Tap(noteId++, (laneBase + 1) % 6, start + (2d * beatDuration), phrase));

                notes.Add(Tap(noteId++, (laneBase + 2) % 6, start + (3d * beatDuration), phrase));

                notes.Add(Hold(noteId++, (laneBase + 3) % 6, start + (4d * beatDuration), HoldBeats * beatDuration, phrase));
                notes.Add(Tap(noteId++, (laneBase + 4) % 6, start + (6d * beatDuration), phrase));
                notes.Add(Tap(noteId++, (laneBase + 5) % 6, start + (7d * beatDuration), phrase));
            }

            notes.Add(Tap(noteId, phraseCount % 6, durationSeconds, phraseCount));
            return notes.ToArray();
        }

        private static NoteData[] CreateHard(double bpm, double durationSeconds)
        {

            double beatDuration = 60d / bpm;
            double phraseDuration = beatDuration * BeatsPerPhrase;
            int phraseCount = (int)Math.Floor(durationSeconds / phraseDuration);
            var notes = new List<NoteData>((phraseCount * 10) + 1);
            int noteId = 1;

            for (int phrase = 0; phrase < phraseCount; phrase++)
            {
                double start = phrase * phraseDuration;
                notes.Add(Tap(noteId++, LeftHandLane, start, phrase));
                notes.Add(Tap(noteId++, BodyLane, start + (0.5d * beatDuration), phrase));
                notes.Add(Tap(noteId++, RightHandLane, start + beatDuration, phrase));
                bool chordPhrase = (phrase & 1) == 1;
                if (chordPhrase)
                {
                    int chordPhraseIndex = phrase / 2;
                    int firstMask = KeyboardChordLayout.GetAllowedMask(chordPhraseIndex * 2);
                    notes.Add(Chord(noteId++, FirstLane(firstMask), firstMask, start + (2d * beatDuration), phrase));
                }
                else
                {
                    notes.Add(Tap(noteId++, LeftFootLane, start + (2d * beatDuration), phrase));
                }
                notes.Add(Tap(noteId++, HeadLane, start + (3d * beatDuration), phrase));
                if (chordPhrase)
                {
                    notes.Add(Tap(noteId++, BodyLane, start + (4d * beatDuration), phrase));
                }
                else
                {
                    notes.Add(Hold(noteId++, BodyLane, start + (4d * beatDuration), HoldBeats * beatDuration, phrase));
                }
                notes.Add(Tap(noteId++, phrase % 2 == 0 ? LeftHandLane : RightHandLane,
                    start + (5d * beatDuration), phrase));
                notes.Add(Tap(noteId++, LeftFootLane, start + (6d * beatDuration), phrase));
                if (chordPhrase)
                {
                    int chordPhraseIndex = phrase / 2;
                    int secondMask = KeyboardChordLayout.GetAllowedMask((chordPhraseIndex * 2) + 1);
                    notes.Add(Chord(noteId++, FirstLane(secondMask), secondMask, start + (6.5d * beatDuration), phrase));
                }
                else
                {
                    notes.Add(Tap(noteId++, RightHandLane, start + (6.5d * beatDuration), phrase));
                }
                notes.Add(Tap(noteId++, RightFootLane, start + (7d * beatDuration), phrase));
            }

            notes.Add(Tap(noteId, BodyLane, durationSeconds, phraseCount));
            notes.Sort((left, right) => left.TimeSec.CompareTo(right.TimeSec));
            return notes.ToArray();
        }

        private static NoteData[] CreateEasy(double durationSeconds)
        {
            var notes = new List<NoteData>((int)Math.Ceiling(durationSeconds));
            int noteId = 1;
            int second = 0;
            while (second < durationSeconds)
            {
                int lane = second % 6;
                int segmentId = second / BeatsPerPhrase;
                notes.Add(Tap(noteId++, lane, second, segmentId));
                second++;
            }

            return notes.ToArray();
        }

        private static NoteData Tap(int id, int lane, double timeSec, int segmentId)
        {
            return new NoteData(id, "tap", lane, timeSec, segmentId: segmentId);
        }

        private static NoteData Hold(int id, int lane, double timeSec, double durationSec, int segmentId)
        {
            return new NoteData(id, "hold", lane, timeSec, durationSec, segmentId);
        }

        private static NoteData Chord(int id, int primaryLane, int mask, double timeSec, int segmentId)
        {
            return new NoteData(
                id,
                "chord",
                primaryLane,
                timeSec,
                segmentId: segmentId,
                requiredLanesMask: mask);
        }

        private static int FirstLane(int mask)
        {
            for (int lane = 0; lane < 6; lane++)
            {
                if ((mask & (1 << lane)) != 0)
                {
                    return lane;
                }
            }

            throw new ArgumentException("Chord mask must contain at least one lane.", nameof(mask));
        }
    }
}
