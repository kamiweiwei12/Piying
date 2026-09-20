using System;
using NUnit.Framework;
using YingYun.Rhythm.Timing;

namespace YingYun.Rhythm.Tests
{
    public sealed class ClockBridgeTests
    {
        [Test]
        public void InputTimeToDsp_UsesCapturedClockOffset()
        {
            var bridge = new ClockBridge(smoothing: 1d);
            bridge.Capture(dspTime: 125d, realtimeSinceStartup: 25d);

            double mapped = bridge.InputTimeToDsp(inputEventTime: 27.5d);

            Assert.That(mapped, Is.EqualTo(127.5d).Within(0.0000001d));
        }

        [Test]
        public void Capture_SmoothsSubsequentOffsetSamples()
        {
            var bridge = new ClockBridge(smoothing: 0.25d);
            bridge.Capture(dspTime: 110d, realtimeSinceStartup: 10d);
            bridge.Capture(dspTime: 112d, realtimeSinceStartup: 10d);

            Assert.That(bridge.DspMinusRealtime, Is.EqualTo(100.5d).Within(0.0000001d));
        }

        [Test]
        public void InputTimeToDsp_BeforeCaptureThrows()
        {
            var bridge = new ClockBridge();

            Assert.Throws<InvalidOperationException>(() => bridge.InputTimeToDsp(1d));
        }
    }
}
