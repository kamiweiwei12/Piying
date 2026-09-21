using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.View;

namespace YingYun.Rhythm.Tests
{
    public sealed class ShadowPuppetPresenterTests
    {
        [Test]
        public void Presenter_BuildsJointedPuppetAndSixControlStrings()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();

            Assert.That(presenter.JointCount, Is.EqualTo(10));
            Assert.That(presenter.StringCount, Is.EqualTo(6));
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Left Shoulder/Joint Left Elbow"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Right Hip/Joint Right Knee"), Is.Not.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RawChordPress_TightensBothStringsAndRotatesBothShoulders()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnInput(new HitInput(1d, 0, InputKind.Press));
            presenter.OnInput(new HitInput(1d, 2, InputKind.Press));
            presenter.Tick(1d);

            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-50f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.RightUpperArmRotation), Is.GreaterThan(50f));
            Assert.That(presenter.GetStringTension(0), Is.EqualTo(1f));
            Assert.That(presenter.GetStringTension(2), Is.EqualTo(1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void MissedPress_StillPullsPuppetImmediately()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnInput(new HitInput(1d, 0, InputKind.Press));
            presenter.OnJudged(new JudgmentResult(
                JudgmentEventKind.NoteJudged,
                1,
                0,
                JudgmentGrade.Miss,
                120d,
                0,
                0,
                0d,
                1 << 0));
            presenter.Tick(1d);

            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-50f));
            Assert.That(presenter.GetStringTension(0), Is.EqualTo(1f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void AutomaticMissWithoutInput_DoesNotMovePuppet()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnJudged(new JudgmentResult(
                JudgmentEventKind.NoteJudged,
                1,
                0,
                JudgmentGrade.Miss,
                0d,
                0,
                0,
                0d,
                1 << 4));
            presenter.Tick(1d);

            Assert.That(Mathf.DeltaAngle(0f, presenter.HeadRotation), Is.Zero.Within(0.0001f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.TorsoRotation), Is.Zero.Within(0.0001f));
            Assert.That(presenter.GetStringTension(4), Is.Zero);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void HoldStarted_KeepsControlStringTautAcrossTime()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(2d);
            presenter.OnInput(new HitInput(2d, 4, InputKind.Press));
            presenter.OnJudged(new JudgmentResult(
                JudgmentEventKind.HoldStarted,
                5,
                0,
                JudgmentGrade.Perfect,
                0d,
                0,
                0,
                0d,
                1 << 4));
            presenter.Tick(5d);

            Assert.That(presenter.GetStringTension(4), Is.GreaterThan(0.95f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ChordPose_RendersVisualAcceptanceImage()
        {
            var root = new GameObject("Puppet Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnInput(new HitInput(1d, 0, InputKind.Press));
            presenter.OnInput(new HitInput(1d, 2, InputKind.Press));
            presenter.Tick(1d);

            var cameraObject = new GameObject("Validation Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);

            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            capture.Apply();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Logs", "M6-puppet-chord.png");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, capture.EncodeToPNG());

            Assert.That(new FileInfo(output).Length, Is.GreaterThan(10000));

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
        }
    }
}
