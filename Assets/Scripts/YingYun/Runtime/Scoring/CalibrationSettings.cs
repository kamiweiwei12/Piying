using System;

namespace YingYun.Rhythm.Scoring
{
    public sealed class CalibrationSettings
    {
        public const double MinimumMs = -300d;
        public const double MaximumMs = 300d;

        public CalibrationSettings(double audioOffsetMs = 0d, double inputOffsetMs = 0d)
        {
            AudioOffsetMs = Clamp(audioOffsetMs);
            InputOffsetMs = Clamp(inputOffsetMs);
        }

        public double AudioOffsetMs { get; private set; }
        public double InputOffsetMs { get; private set; }

        public void AdjustAudio(double deltaMs) => AudioOffsetMs = Clamp(AudioOffsetMs + deltaMs);
        public void AdjustInput(double deltaMs) => InputOffsetMs = Clamp(InputOffsetMs + deltaMs);

        private static double Clamp(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return Math.Max(MinimumMs, Math.Min(MaximumMs, value));
        }
    }
}
