using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>六部位放射式音符的純位置計算。</summary>
    public static class RadialNoteGeometry
    {
        public static float Progress(double songTimeSec, double noteTimeSec, double visibleLeadSec)
        {
            if (visibleLeadSec <= 0d)
            {
                return songTimeSec >= noteTimeSec ? 1f : 0f;
            }

            return Mathf.Clamp01((float)(1d - ((noteTimeSec - songTimeSec) / visibleLeadSec)));
        }

        public static Vector2 Position(
            Vector2 spawnPosition,
            Vector2 receptorPosition,
            double songTimeSec,
            double noteTimeSec,
            double visibleLeadSec)
        {
            return Vector2.LerpUnclamped(
                spawnPosition,
                receptorPosition,
                Progress(songTimeSec, noteTimeSec, visibleLeadSec));
        }
    }
}
