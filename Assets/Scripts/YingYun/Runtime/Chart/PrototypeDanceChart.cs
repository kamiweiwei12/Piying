using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;

namespace YingYun.Rhythm.Chart
{
    /// <summary>建立 M5 原型使用的八拍操偶舞句。所有時間均由 BPM 換算為秒。</summary>
    public static class PrototypeDanceChart
    {
        public const int LeftHandLane = 0;
        public const int HeadLane = 1;
        public const int RightHandLane = 2;
        public const int LeftFootLane = 3;
        public const int BodyLane = 4;
        public const int RightFootLane = 5;

        private const int BeatsPerPhrase = 8;

        public static NoteData[] Create(double bpm, double durationSeconds)
        {
            if (bpm <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            if (durationSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            }

            double beatDuration = 60d / bpm;
            double phraseDuration = beatDuration * BeatsPerPhrase;
            int phraseCount = (int)Math.Floor(durationSeconds / phraseDuration);
            var notes = new List<NoteData>((phraseCount * 7) + 1);
            int noteId = 1;

            for (int phrase = 0; phrase < phraseCount; phrase++)
            {
                double start = phrase * phraseDuration;
                notes.Add(Tap(noteId++, LeftHandLane, start, phrase));
                notes.Add(Tap(noteId++, RightHandLane, start + beatDuration, phrase));
                notes.Add(Chord(
                    noteId++,
                    LeftHandLane,
                    Mask(LeftHandLane, RightHandLane),
                    start + (2d * beatDuration),
                    phrase));
                notes.Add(Tap(noteId++, HeadLane, start + (3d * beatDuration), phrase));
                notes.Add(new NoteData(
                    noteId++,
                    "hold",
                    BodyLane,
                    start + (4d * beatDuration),
                    beatDuration,
                    phrase));
                notes.Add(Tap(noteId++, LeftFootLane, start + (6d * beatDuration), phrase));
                notes.Add(Tap(noteId++, RightFootLane, start + (7d * beatDuration), phrase));
            }

            int finaleSegment = phraseCount;
            notes.Add(Chord(
                noteId,
                LeftHandLane,
                Mask(LeftHandLane, RightHandLane),
                durationSeconds,
                finaleSegment));
            return notes.ToArray();
        }

        private static NoteData Tap(int id, int lane, double timeSec, int segmentId)
        {
            return new NoteData(id, "tap", lane, timeSec, segmentId: segmentId);
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

        private static int Mask(int firstLane, int secondLane)
        {
            return (1 << firstLane) | (1 << secondLane);
        }
    }
}
