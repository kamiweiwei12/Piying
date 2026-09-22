using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class NorthernShadowLegSolverTests
    {
        [TestCase(-0.27f, -1.60f)]
        [TestCase(0.10f, -1.40f)]
        [TestCase(0.48f, -1.20f)]
        public void TwoSegmentSolution_ReachesAnkleAndBendsKnee(float x, float y)
        {
            var ankle = new Vector2(x, y);
            NorthernShadowLegSolver.Solve(Vector2.zero, ankle, 1f,
                out float thigh, out float knee);
            float upper = thigh * Mathf.Deg2Rad;
            float lower = (thigh + knee) * Mathf.Deg2Rad;
            Vector2 reached = new Vector2(
                (Mathf.Sin(upper) * NorthernShadowLegSolver.ThighLength) +
                (Mathf.Sin(lower) * NorthernShadowLegSolver.ShinLength),
                -(Mathf.Cos(upper) * NorthernShadowLegSolver.ThighLength) -
                (Mathf.Cos(lower) * NorthernShadowLegSolver.ShinLength));
            Assert.That(Vector2.Distance(reached, ankle), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(knee), Is.GreaterThan(5f));
        }
    }
}
