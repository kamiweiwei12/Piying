using UnityEngine;

namespace YingYun.Rhythm.View
{
    /// <summary>幕面平面內的兩節影人腿。足踝目標固定時由髖位求膝位，避免腳片滑走。</summary>
    public static class NorthernShadowLegSolver
    {
        public const float ThighLength = 0.88f;
        public const float ShinLength = 0.82f;

        public static void Solve(Vector2 hip, Vector2 ankle, float bendSide,
            out float thighDegrees, out float kneeDegrees)
        {
            Vector2 span = ankle - hip;
            float distance = Mathf.Clamp(span.magnitude, 0.12f, ThighLength + ShinLength - 0.015f);
            Vector2 direction = span.sqrMagnitude < 0.0001f ? Vector2.down : span.normalized;
            float along = ((ThighLength * ThighLength) - (ShinLength * ShinLength) +
                (distance * distance)) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, (ThighLength * ThighLength) - (along * along)));
            Vector2 knee = hip + (direction * along) +
                (new Vector2(-direction.y, direction.x) * height * Mathf.Sign(bendSide));
            Vector2 upper = knee - hip;
            Vector2 lower = ankle - knee;
            thighDegrees = Mathf.Atan2(upper.x, -upper.y) * Mathf.Rad2Deg;
            float shinWorldDegrees = Mathf.Atan2(lower.x, -lower.y) * Mathf.Rad2Deg;
            kneeDegrees = Mathf.DeltaAngle(thighDegrees, shinWorldDegrees);
        }
    }
}
