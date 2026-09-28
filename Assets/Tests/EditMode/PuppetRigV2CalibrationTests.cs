using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetRigV2CalibrationTests
    {
        private const float SweepAngle = 12f;

        [Test]
        public void P0_ContractMatchesCurrentFifteenPieceRig()
        {
            var root = new GameObject("V2 P0 Contract Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Transform stage = root.transform.Find("M6 Shadow Puppet Stage");

            Assert.That(presenter.UsesSegmentedPuppetArt, Is.True);
            Assert.That(presenter.PuppetArtRendererCount, Is.EqualTo(15));
            Assert.That(PuppetRigV2Calibration.Art.Length, Is.EqualTo(15));
            Assert.That(PuppetRigV2Calibration.Joints.Length, Is.EqualTo(14));
            foreach (PuppetArtCalibration art in PuppetRigV2Calibration.Art)
            {
                Transform part = stage.Find(art.Path);
                Assert.That(part, Is.Not.Null, art.Path);
                Assert.That(part.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo(art.SpriteName));
                Assert.That(Mathf.DeltaAngle(art.BindAngle, part.localEulerAngles.z), Is.Zero.Within(0.02f));
            }
            foreach (PuppetJointCalibration joint in PuppetRigV2Calibration.Joints)
                Assert.That(stage.Find(joint.Path), Is.Not.Null, joint.Path);

            PuppetRigCalibrationSnapshot snapshot = presenter.CaptureV2Calibration();
            Assert.That(snapshot.VisibleHeight, Is.GreaterThan(4f));
            Assert.That(PuppetRigV2Calibration.StageSafetyFrame.Contains(snapshot.LeftFingertip), Is.True);
            Assert.That(PuppetRigV2Calibration.StageSafetyFrame.Contains(snapshot.RightFingertip), Is.True);
            Assert.That(PuppetRigV2Calibration.ArmMaximumReach, Is.EqualTo(1.54f).Within(0.0001f));
            Assert.That(PuppetRigV2Calibration.LegMaximumReach, Is.EqualTo(1.70f).Within(0.0001f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void P0_EachJointSmallAngleSweepKeepsRivetsConnected_AndWritesAnnotatedEvidence()
        {
            var root = new GameObject("V2 P0 Sweep Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Transform stage = root.transform.Find("M6 Shadow Puppet Stage");
            PuppetRigCalibrationSnapshot snapshot = presenter.CaptureV2Calibration();
            var trails = new List<Vector3[]>();

            foreach (PuppetJointCalibration spec in PuppetRigV2Calibration.Joints)
            {
                Transform joint = stage.Find(spec.Path);
                Transform endpoint = EndpointFor(joint);
                Quaternion bind = joint.localRotation;
                float distance = Vector3.Distance(joint.position, endpoint.position);
                var trail = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    joint.localRotation = bind * Quaternion.Euler(0f, 0f, (i - 1) * SweepAngle);
                    trail[i] = stage.InverseTransformPoint(endpoint.position);
                    Assert.That(Vector3.Distance(joint.position, endpoint.position), Is.EqualTo(distance).Within(0.0001f),
                        spec.Label + " sweep split its rivet chain");
                }
                joint.localRotation = bind;
                trails.Add(trail);
            }

            GameObject overlay = BuildOverlay(stage, snapshot, trails);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string evidenceDirectory = Path.Combine(projectRoot, "Logs", "PuppetV2", "P0");
            Directory.CreateDirectory(evidenceDirectory);
            string imagePath = Path.Combine(evidenceDirectory, "P0-rig-calibration-and-joint-sweeps.png");
            Capture(imagePath);
            File.WriteAllText(Path.Combine(evidenceDirectory, "P0-calibration-report.md"),
                BuildReport(stage, snapshot), Encoding.UTF8);

            Assert.That(new FileInfo(imagePath).Length, Is.GreaterThan(20000));
            Object.DestroyImmediate(overlay);
            Object.DestroyImmediate(root);
        }

        private static Transform EndpointFor(Transform joint)
        {
            string name = joint.name;
            if (name.Contains("Shoulder")) return joint.Find(name.Contains("Left") ? "Joint Left Elbow" : "Joint Right Elbow");
            if (name.Contains("Elbow")) return joint.Find(name.Contains("Left") ? "Joint Left Wrist" : "Joint Right Wrist");
            if (name.Contains("Hip")) return joint.Find(name.Contains("Left") ? "Joint Left Knee" : "Joint Right Knee");
            if (name.Contains("Knee")) return joint.Find(name.Contains("Left") ? "Joint Left Ankle" : "Joint Right Ankle");
            if (name.Contains("Wrist")) return joint.Find(name.Contains("Left") ? "Art Left Hand" : "Art Right Hand");
            if (name.Contains("Ankle")) return joint.Find(name.Contains("Left") ? "Art Left Shoe" : "Art Right Shoe");
            if (name.Contains("Waist")) return joint.Find("Joint Neck");
            return joint.Find("Art Head");
        }

        private static GameObject BuildOverlay(Transform stage, PuppetRigCalibrationSnapshot snapshot,
            IReadOnlyList<Vector3[]> trails)
        {
            var overlay = new GameObject("V2 P0 Calibration Overlay");
            overlay.transform.SetParent(stage, false);
            Material material = new Material(Shader.Find("Sprites/Default"));
            AddLine(overlay.transform, "safe frame", material, new Color(0.05f, 0.95f, 0.85f, 0.9f),
                new Vector3(PuppetRigV2Calibration.StageSafetyFrame.xMin, PuppetRigV2Calibration.StageSafetyFrame.yMin),
                new Vector3(PuppetRigV2Calibration.StageSafetyFrame.xMin, PuppetRigV2Calibration.StageSafetyFrame.yMax),
                new Vector3(PuppetRigV2Calibration.StageSafetyFrame.xMax, PuppetRigV2Calibration.StageSafetyFrame.yMax),
                new Vector3(PuppetRigV2Calibration.StageSafetyFrame.xMax, PuppetRigV2Calibration.StageSafetyFrame.yMin),
                new Vector3(PuppetRigV2Calibration.StageSafetyFrame.xMin, PuppetRigV2Calibration.StageSafetyFrame.yMin));
            for (int i = 0; i < PuppetRigV2Calibration.Joints.Length; i++)
            {
                PuppetJointCalibration spec = PuppetRigV2Calibration.Joints[i];
                Transform joint = stage.Find(spec.Path);
                AddMarker(overlay.transform, material, stage.InverseTransformPoint(joint.position), 0.065f,
                    spec.PositiveAxis < 0f ? new Color(0.1f, 0.8f, 1f, 1f) : new Color(1f, 0.45f, 0.1f, 1f));
                AddLine(overlay.transform, "sweep " + spec.Label, material, new Color(1f, 1f, 0.1f, 0.9f), trails[i]);
            }
            AddMarker(overlay.transform, material, snapshot.LeftFingertip, 0.08f, Color.magenta);
            AddMarker(overlay.transform, material, snapshot.RightFingertip, 0.08f, Color.magenta);
            AddMarker(overlay.transform, material, snapshot.LeftSole, 0.08f, Color.green);
            AddMarker(overlay.transform, material, snapshot.RightSole, 0.08f, Color.green);
            return overlay;
        }

        private static void AddMarker(Transform parent, Material material, Vector3 position, float radius, Color color)
        {
            const int segments = 16;
            var points = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.PI * 2f * i / segments;
                points[i] = position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            }
            AddLine(parent, "calibration marker", material, color, points);
        }

        private static void AddLine(Transform parent, string name, Material material, Color color, params Vector3[] points)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.material = material;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = 40;
        }

        private static string BuildReport(Transform stage, PuppetRigCalibrationSnapshot snapshot)
        {
            var text = new StringBuilder();
            text.AppendLine("# V2-P0 current 15-piece rig calibration");
            text.AppendLine();
            text.AppendLine("Coordinates are local to M6 Shadow Puppet Stage. Left/right are character chains.");
            text.AppendLine();
            text.AppendLine($"- visible bounds: {Point(snapshot.VisibleBounds.min)} to {Point(snapshot.VisibleBounds.max)}");
            text.AppendLine($"- visible height H: {snapshot.VisibleHeight.ToString("F4", CultureInfo.InvariantCulture)}");
            text.AppendLine($"- left/right fingertip proxies: {Point(snapshot.LeftFingertip)}, {Point(snapshot.RightFingertip)}");
            text.AppendLine($"- left/right sole contacts: {Point(snapshot.LeftSole)}, {Point(snapshot.RightSole)}");
            text.AppendLine($"- safe frame: {PuppetRigV2Calibration.StageSafetyFrame}");
            text.AppendLine($"- arm reach annulus: {PuppetRigV2Calibration.ArmMinimumReach:F2}..{PuppetRigV2Calibration.ArmMaximumReach:F2}");
            text.AppendLine($"- leg reach annulus: {PuppetRigV2Calibration.LegMinimumReach:F2}..{PuppetRigV2Calibration.LegMaximumReach:F2}");
            text.AppendLine();
            text.AppendLine("| joint | position | zero angle | positive screen axis |");
            text.AppendLine("|---|---:|---:|---:|");
            foreach (PuppetJointCalibration spec in PuppetRigV2Calibration.Joints)
            {
                Transform joint = stage.Find(spec.Path);
                text.AppendLine($"| {spec.Label} | {Point(stage.InverseTransformPoint(joint.position))} | {spec.BindAngle:F1} deg | {(spec.PositiveAxis < 0 ? "left/CCW mapping" : "right/CW mapping")} |");
            }
            return text.ToString();
        }

        private static string Point(Vector3 point) => string.Format(CultureInfo.InvariantCulture, "({0:F4}, {1:F4})", point.x, point.y);

        private static void Capture(string path)
        {
            var cameraObject = new GameObject("V2 P0 Evidence Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0f, 0f, 1600f, 1000f), 0, 0);
            capture.Apply();
            File.WriteAllBytes(path, capture.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
