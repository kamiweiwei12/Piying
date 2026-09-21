using NUnit.Framework;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Tests
{
    public sealed class CalibrationSettingsTests
    {
        [Test]
        public void Adjust_TracksAudioAndInputIndependently()
        {
            var settings = new CalibrationSettings(10d, -5d);

            settings.AdjustAudio(5d);
            settings.AdjustInput(-10d);

            Assert.That(settings.AudioOffsetMs, Is.EqualTo(15d));
            Assert.That(settings.InputOffsetMs, Is.EqualTo(-15d));
        }

        [Test]
        public void Adjust_ClampsOffsetsToSafeRange()
        {
            var settings = new CalibrationSettings(999d, -999d);

            Assert.That(settings.AudioOffsetMs, Is.EqualTo(CalibrationSettings.MaximumMs));
            Assert.That(settings.InputOffsetMs, Is.EqualTo(CalibrationSettings.MinimumMs));
        }
    }
}
