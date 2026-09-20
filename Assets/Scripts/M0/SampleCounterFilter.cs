// ============================================================================
// 《影韵》M0-A 音訊取樣計數器（拋棄式量測工具 · 單一職責）
// ----------------------------------------------------------------------------
// 唯一職責：實作 OnAudioFilterRead 並累計「已消耗的 sample frames」。
//
// 【重要】本元件必須與 AudioSource 掛在同一個 GameObject 上，且該 GameObject
// 【不得】同時擁有 AudioListener。原因：Unity 對 OnAudioFilterRead 的綁定規則是
// 「同一 GameObject 上只能有 AudioSource 或 AudioListener 其一」；若兩者並存，
// Unity 會先綁到 AudioListener（收到最終混音 final mix，而非該來源的音訊流），
// 並印出「GameObject has multiple AudioSources and/or AudioListeners attached」，
// 導致 consumedSamples 在 AudioSource 被暫停時仍持續增加，量測失真。
//
// 本元件【不含】任何 M0 測試邏輯（按鍵、CSV、Input System、scenario 皆在 ClockProbe）。
// ============================================================================

using System.Threading;
using UnityEngine;

namespace YingYun.M0
{
    /// <summary>
    /// 音訊執行緒取樣計數器：只統計 consumed sample frames 與最後看到的聲道數。
    /// </summary>
    public sealed class SampleCounterFilter : MonoBehaviour
    {
        private long _consumedFrames;
        private int _lastChannels;

        /// <summary>累計消耗的 sample frames（= 單聲道取樣數）；可由主執行緒安全讀取。</summary>
        public long ConsumedFrames
        {
            get { return Interlocked.Read(ref _consumedFrames); }
        }

        /// <summary>最後一次 OnAudioFilterRead 看到的聲道數（僅記錄用）。</summary>
        public int LastChannels
        {
            get { return Volatile.Read(ref _lastChannels); }
        }

        /// <summary>歸零計數（不影響播放）。</summary>
        public void ResetCounter()
        {
            Interlocked.Exchange(ref _consumedFrames, 0L);
            Volatile.Write(ref _lastChannels, 0);
        }

        /// <summary>
        /// 音訊執行緒：不可呼叫任何 Unity API，只做純計算與原子累加。
        /// data.Length / channels = 本次回呼消耗的 sample frames。
        /// </summary>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels <= 0)
            {
                channels = 1;
            }

            Volatile.Write(ref _lastChannels, channels);
            Interlocked.Add(ref _consumedFrames, data.Length / channels);
        }
    }
}
