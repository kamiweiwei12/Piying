using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>幕面内两节手臂目标求解；输出从垂直向下零轴开始的肩角和局部肘角。</summary>
    public static class PlanarTwoBoneArmSolver
    {
        public static void Solve(Vector2 shoulder, Vector2 wrist, float upperLength,
            float forearmLength, float bendSide, out float shoulderDegrees, out float elbowDegrees)
        {
            Vector2 span = wrist - shoulder;
            float minimum = Mathf.Abs(upperLength - forearmLength) + 0.001f;
            float maximum = upperLength + forearmLength - 0.001f;
            float distance = Mathf.Clamp(span.magnitude, minimum, maximum);
            Vector2 direction = span.sqrMagnitude < 0.0001f ? Vector2.down : span.normalized;
            float along = ((upperLength * upperLength) - (forearmLength * forearmLength) +
                (distance * distance)) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, (upperLength * upperLength) - (along * along)));
            Vector2 elbow = shoulder + (direction * along) +
                (new Vector2(-direction.y, direction.x) * height * Mathf.Sign(bendSide));
            Vector2 upper = elbow - shoulder;
            Vector2 lower = wrist - elbow;
            shoulderDegrees = Mathf.Atan2(upper.x, -upper.y) * Mathf.Rad2Deg;
            float forearmWorldDegrees = Mathf.Atan2(lower.x, -lower.y) * Mathf.Rad2Deg;
            elbowDegrees = Mathf.DeltaAngle(shoulderDegrees, forearmWorldDegrees);
        }
    }
}
