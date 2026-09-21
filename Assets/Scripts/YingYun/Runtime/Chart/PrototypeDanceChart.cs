using System;
using System.Collections.Generic;
using YingYun.Rhythm.Judgment;

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

        /// <summary>
        /// 輪替長按的部位順序。身體（S）不在其中：S 每個樂句都固定有「長按 + 點按」，
        /// 其餘五個部位每 5 個樂句（20 秒）輪到一次。
        /// </summary>
        private static readonly int[] RotationLanes =
        {
            LeftHandLane, RightHandLane, HeadLane, LeftFootLane, RightFootLane
        };

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

                // 輪替長按：手／頭落在第 5–7 拍，腳落在第 2–4 拍，
                // 都避開同軌的點按，避免出現「按住時還要再打同一顆」的無解音符。
                int featuredLane = RotationLanes[phrase % RotationLanes.Length];
                bool footHold = featuredLane == LeftFootLane || featuredLane == RightFootLane;
                bool handOrHeadHold = !footHold;

                notes.Add(Tap(noteId++, LeftHandLane, start, phrase));
                notes.Add(Tap(noteId++, RightHandLane, start + beatDuration, phrase));
                notes.Add(Chord(
                    noteId++,
                    LeftHandLane,
                    Mask(LeftHandLane, RightHandLane),
                    start + (2d * beatDuration),
                    phrase));

                if (footHold)
                {
                    notes.Add(Hold(
                        noteId++,
                        featuredLane,
                        start + (2d * beatDuration),
                        HoldBeats * beatDuration,
                        phrase));
                }

                notes.Add(Tap(noteId++, HeadLane, start + (3d * beatDuration), phrase));
                notes.Add(Hold(
                    noteId++,
                    BodyLane,
                    start + (4d * beatDuration),
                    HoldBeats * beatDuration,
                    phrase));

                if (handOrHeadHold)
                {
                    notes.Add(Hold(
                        noteId++,
                        featuredLane,
                        start + (5d * beatDuration),
                        HoldBeats * beatDuration,
                        phrase));
                }

                notes.Add(Tap(noteId++, LeftFootLane, start + (6d * beatDuration), phrase));
                notes.Add(Tap(noteId++, RightFootLane, start + (7d * beatDuration), phrase));
                notes.Add(Tap(noteId++, BodyLane, start + (7d * beatDuration), phrase));
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

        private static int Mask(int firstLane, int secondLane)
        {
            return (1 << firstLane) | (1 << secondLane);
        }
    }
}
