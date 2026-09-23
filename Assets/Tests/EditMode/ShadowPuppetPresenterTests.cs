using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class ShadowPuppetPresenterTests
    {
        private const double FrameSeconds = 1d / 60d;

        [Test]
        public void DanceMode_OnlySuccessfulAnchorMovesAndTurnFlipsProfile()
        {
            var root = new GameObject("M8 Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            presenter.Begin(phrases);
            presenter.Tick(0d);
            presenter.OnInput(new HitInput(0d, 0, InputKind.Press));
            presenter.Tick(1d);
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.Zero.Within(0.01f));

            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                phrases[0].AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            presenter.Tick(2d);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation)), Is.GreaterThan(20f));
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Joint Neck/Profile Nose"), Is.Not.Null);

            DancePhrase turn = phrases[3];
            presenter.Tick(turn.StartSeconds);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                turn.AnchorNoteId, 0, JudgmentGrade.Great, 0d, 2, 2000, 1d));
            presenter.Tick(turn.StartSeconds + turn.DurationSeconds);
            Assert.That(presenter.FacingScale, Is.LessThan(-0.9f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void DanceMode_HitDrivesActualLowerBodyAndFootRod()
        {
            var root = new GameObject("M8 Play Foot Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            presenter.Begin(phrases);
            presenter.Tick(0d);
            Vector3 planted = presenter.LeftAnklePosition;
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                phrases[0].AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            presenter.Tick(2d);
            Assert.That(Vector3.Distance(planted, presenter.LeftAnklePosition), Is.LessThan(0.025f));
            Assert.That(presenter.RightAnklePosition.y, Is.GreaterThan(-1.80f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.RightShinRotation)), Is.GreaterThan(10f));
            Assert.That(presenter.GetRodDrive(5), Is.GreaterThan(0.8f));
            presenter.Tick(4d);
            Assert.That(presenter.RightAnklePosition.y, Is.EqualTo(-2.08f).Within(0.025f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void DanceMode_PlaybackStepIsContinuousAndRendersAcceptanceImage()
        {
            var root = new GameObject("M8 Song Step Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            presenter.Begin(phrases);
            presenter.Tick(0d);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                phrases[0].AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            Vector3 prior = presenter.RightAnklePosition;
            float largestFrameMove = 0f;
            for (int frame = 1; frame <= 120; frame++)
            {
                presenter.Tick(frame * FrameSeconds);
                largestFrameMove = Mathf.Max(largestFrameMove,
                    Vector3.Distance(prior, presenter.RightAnklePosition));
                prior = presenter.RightAnklePosition;
            }
            Assert.That(largestFrameMove, Is.LessThan(0.025f));

            var cameraObject = new GameObject("Song Step Camera");
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
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "M8-R-song-step-mid.png");
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

        [Test]
        public void DanceMode_HoldMaintainsRodAndSleeveTensionUntilJudged()
        {
            var root = new GameObject("M8 Hold Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            presenter.Begin(phrases);
            presenter.Tick(6d);
            var sleeve = root.transform.Find(
                "M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Joint Left Shoulder/Left Flowing Sleeve");
            Assert.That(sleeve, Is.Not.Null);

            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.HoldStarted,
                99999, 1, JudgmentGrade.Great, 0d, 1, 0, 0d, 1));
            presenter.Tick(6.5d);
            Assert.That(presenter.GetRodDrive(0), Is.GreaterThanOrEqualTo(0.75f));
            Assert.That(sleeve.localScale.y, Is.GreaterThan(1f));
            presenter.Tick(7d);
            Assert.That(sleeve.localScale.y, Is.GreaterThan(1f));

            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                99999, 1, JudgmentGrade.Perfect, 0d, 1, 0, 0d, 1));
            presenter.Tick(7.5d);
            Assert.That(sleeve.localScale.y, Is.LessThan(0.85f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Presenter_BuildsJointedShadowFigureAndSixBambooRods()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();

            Assert.That(presenter.JointCount, Is.EqualTo(21));
            Assert.That(presenter.RodCount, Is.EqualTo(6));
            Assert.That(presenter.HasBackgroundPicture, Is.True);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Traditional Shadow Play Background"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Right Hip/Joint Right Knee"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow/Joint Left Wrist/Joint Left Finger Fan"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Joint Left Shoulder/Joint Left Elbow/Joint Left Sleeve Cuff/Joint Left Sleeve Tail"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Right Hip/Joint Right Knee/Joint Right Ankle/Right Foot Plate"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Bamboo Control Rod 0"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Control String 0"), Is.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Stage Header"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Robe Skirt"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Joint Neck/Crown Wing Left"), Is.Not.Null);
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Waist/Chest Rod Socket"), Is.Not.Null);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void RawPress_TightensTheStringAndStartsMovingTheJointWithinThreeFrames()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnInput(new HitInput(1d, 0, InputKind.Press));
            presenter.Tick(1d);

            Assert.That(presenter.GetRodDrive(0), Is.EqualTo(1f));

            presenter.Tick(1d + (3d * FrameSeconds));
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-3f));

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

            // 繩索一按下就繃緊；關節帶慣性，約 0.3 秒後到達高點。
            Assert.That(presenter.GetRodDrive(0), Is.EqualTo(1f));
            Assert.That(presenter.GetRodDrive(2), Is.EqualTo(1f));

            presenter.Tick(1.35d);
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-45f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.RightUpperArmRotation), Is.GreaterThan(45f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftForearmRotation), Is.LessThan(-12f));
            Assert.That(Mathf.DeltaAngle(0f, presenter.RightForearmRotation), Is.GreaterThan(12f));

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

            Assert.That(presenter.GetStringTension(0), Is.EqualTo(1f));

            presenter.Tick(1.35d);
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-45f));

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
            Assert.That(Mathf.DeltaAngle(0f, presenter.TorsoRotation), Is.GreaterThan(6f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void HoldStarted_KeepsTheArmRaisedUntilRelease()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnInput(new HitInput(1d, 0, InputKind.Press));
            presenter.OnJudged(new JudgmentResult(
                JudgmentEventKind.HoldStarted,
                2,
                0,
                JudgmentGrade.Perfect,
                0d,
                0,
                0,
                0d,
                1 << 0));

            presenter.Tick(1.6d);
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-45f));

            presenter.Tick(2.4d);
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.LessThan(-45f));
            Assert.That(presenter.GetStringTension(0), Is.GreaterThan(0.95f));

            presenter.OnInput(new HitInput(2.5d, 0, InputKind.Release));
            presenter.Tick(3.2d);
            Assert.That(Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation), Is.GreaterThan(-20f));
            Assert.That(presenter.GetStringTension(0), Is.LessThan(0.05f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void HoldStarted_ContinuouslyWorksTheRodInsteadOfFreezingLikeATap()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(1d);
            presenter.OnInput(new HitInput(1d, 0, InputKind.Press));
            presenter.OnJudged(new JudgmentResult(
                JudgmentEventKind.HoldStarted,
                2,
                0,
                JudgmentGrade.Perfect,
                0d,
                0,
                0,
                0d,
                1 << 0));

            presenter.Tick(1.40d);
            Vector3 firstGrip = presenter.GetRodGripPosition(0);
            float firstForearm = presenter.LeftForearmRotation;
            presenter.Tick(1.55d);
            Vector3 secondGrip = presenter.GetRodGripPosition(0);
            float secondForearm = presenter.LeftForearmRotation;

            Assert.That(Vector3.Distance(firstGrip, secondGrip), Is.GreaterThan(0.02f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(firstForearm, secondForearm)), Is.GreaterThan(0.25f));
            Assert.That(presenter.GetRodDrive(0), Is.GreaterThan(0.95f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void Sequence_KeepsThePuppetMovingWithoutSlidingThePelvis()
        {
            var root = new GameObject("Puppet Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(0d);

            Vector3 pelvis = presenter.PelvisPosition;
            double previous = 0d;
            double maxDelta = 0d;
            double earlySequence = 0d;
            double midSequence = 0d;

            // 120 BPM：Q（0.0）→ E（0.5）→ Q+E（1.0）→ D（1.5）
            for (int frame = 0; frame <= 150; frame++)
            {
                double songTime = frame * FrameSeconds;
                if (frame == 0)
                {
                    presenter.OnInput(new HitInput(songTime, 0, InputKind.Press));
                }
                else if (frame == 30)
                {
                    presenter.OnInput(new HitInput(songTime, 2, InputKind.Press));
                }
                else if (frame == 60)
                {
                    presenter.OnInput(new HitInput(songTime, 0, InputKind.Press));
                    presenter.OnInput(new HitInput(songTime, 2, InputKind.Press));
                }
                else if (frame == 90)
                {
                    presenter.OnInput(new HitInput(songTime, 5, InputKind.Press));
                }

                presenter.Tick(songTime);

                double current = Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation);
                if (frame > 0)
                {
                    maxDelta = Math.Max(maxDelta, Math.Abs(current - previous));
                }

                previous = current;
                if (frame == 27)
                {
                    earlySequence = current;
                }
                else if (frame == 75)
                {
                    midSequence = current;
                }
            }

            Assert.That(maxDelta, Is.LessThan(10d));
            Assert.That(earlySequence, Is.LessThan(-30d));
            Assert.That(midSequence, Is.LessThan(-30d));
            Assert.That(Vector3.Distance(pelvis, presenter.PelvisPosition), Is.LessThan(0.0001f));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void SequencePlayback_WritesTrajectoryLog()
        {
            var root = new GameObject("Puppet Trajectory Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.Tick(0d);

            var builder = new StringBuilder();
            builder.AppendLine(
                "songTime,leftShoulder,leftElbow,rightShoulder,rightElbow,leftThigh,leftShin,torso,head,leftTension,rightTension");

            for (int frame = 0; frame <= 150; frame++)
            {
                double songTime = frame * FrameSeconds;
                if (frame == 0)
                {
                    presenter.OnInput(new HitInput(songTime, 0, InputKind.Press));
                }
                else if (frame == 30)
                {
                    presenter.OnInput(new HitInput(songTime, 2, InputKind.Press));
                }
                else if (frame == 60)
                {
                    presenter.OnInput(new HitInput(songTime, 0, InputKind.Press));
                    presenter.OnInput(new HitInput(songTime, 2, InputKind.Press));
                }
                else if (frame == 90)
                {
                    presenter.OnInput(new HitInput(songTime, 3, InputKind.Press));
                }

                presenter.Tick(songTime);
                builder.AppendLine(string.Join(
                    ",",
                    songTime.ToString("F4", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.LeftUpperArmRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.LeftForearmRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.RightUpperArmRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.RightForearmRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.LeftThighRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.LeftShinRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.TorsoRotation).ToString("F3", CultureInfo.InvariantCulture),
                    Mathf.DeltaAngle(0f, presenter.HeadRotation).ToString("F3", CultureInfo.InvariantCulture),
                    presenter.GetStringTension(0).ToString("F3", CultureInfo.InvariantCulture),
                    presenter.GetStringTension(2).ToString("F3", CultureInfo.InvariantCulture)));
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "Logs", "M6-1-puppet-trajectory.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, builder.ToString());

            Assert.That(new FileInfo(output).Length, Is.GreaterThan(4000));

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
            presenter.Tick(1.35d);

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
            string output = Path.Combine(projectRoot, "Logs", "M6-5-shadow-play-chord.png");
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

        [Test]
        public void DanceTurn_RendersFacingBeforeAndAfterImages()
        {
            var root = new GameObject("M8 Turn Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            presenter.Begin(phrases);

            var cameraObject = new GameObject("M8 Validation Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;

            DancePhrase turn = phrases[3];
            presenter.Tick(turn.StartSeconds);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                turn.AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            for (int frame = 0; frame < 3; frame++)
            {
                presenter.Tick(turn.StartSeconds + (frame * turn.DurationSeconds * 0.5d));
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                capture.Apply();
                string output = Path.Combine(projectRoot, "Logs",
                    frame == 0 ? "M8-turn-before.png" : frame == 1 ? "M8-turn-mid.png" : "M8-turn-after.png");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, capture.EncodeToPNG());
                Assert.That(new FileInfo(output).Length, Is.GreaterThan(10000));
            }

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void StructurePreview_PlantedFootStaysPutWhileOtherKneeBends()
        {
            var root = new GameObject("Northern Rig Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Vector3 planted = presenter.LeftAnklePosition;
            presenter.PreviewStructure(0.55f);
            Assert.That(Vector3.Distance(planted, presenter.LeftAnklePosition), Is.LessThan(0.025f));
            Assert.That(presenter.RightAnklePosition.y, Is.GreaterThan(-1.98f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.RightShinRotation)), Is.GreaterThan(8f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.LeftWristRotation)), Is.GreaterThan(2f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.LeftFingerRotation)), Is.GreaterThan(1f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ExperimentalPalmGestures_CreateDistinctWristSilhouettesWithoutMovingFeet()
        {
            var root = new GameObject("M8.1 Hand Gesture Preview Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;

            presenter.PreviewHandGesture(HandGesture.PressPalm, 1f);
            float pressWrist = presenter.LeftWristRotation;
            Vector3 pressPosition = presenter.LeftWristPosition;
            presenter.PreviewHandGesture(HandGesture.SupportPalm, 1f);
            float supportWrist = presenter.LeftWristRotation;
            Vector3 supportPosition = presenter.LeftWristPosition;

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(pressWrist, supportWrist)), Is.GreaterThan(120f));
            Assert.That(supportPosition.y, Is.GreaterThan(pressPosition.y + 0.25f));
            Assert.That(Vector3.Distance(leftFoot, presenter.LeftAnklePosition), Is.LessThan(0.025f));
            Assert.That(Vector3.Distance(rightFoot, presenter.RightAnklePosition), Is.LessThan(0.025f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ExperimentalPalmGestures_RenderComparisonImages()
        {
            var root = new GameObject("M8.1 Palm Gesture Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            var cameraObject = new GameObject("M8.1 Validation Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            HandGesture[] gestures = { HandGesture.PressPalm, HandGesture.SupportPalm };
            string[] names = { "M8-1-press-palm.png", "M8-1-support-palm.png" };
            for (int i = 0; i < gestures.Length; i++)
            {
                presenter.PreviewHandGesture(gestures[i], 1f);
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                capture.Apply();
                string output = Path.Combine(projectRoot, "Logs", names[i]);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, capture.EncodeToPNG());
                Assert.That(new FileInfo(output).Length, Is.GreaterThan(10000));
            }

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ExperimentalThreadPalmAndTurnWrist_RenderCapabilityImagesWithoutMovingFeet()
        {
            var root = new GameObject("M8.1 Thread Palm And Turn Wrist Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            var cameraObject = new GameObject("M8.1 Gesture Validation Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;

            HandGesture[] gestures =
            {
                HandGesture.ThreadPalm, HandGesture.ThreadPalm,
                HandGesture.TurnWrist, HandGesture.TurnWrist
            };
            float[] progress = { 0.34f, 1f, 0.5f, 1f };
            string[] names =
            {
                "M8-1-thread-palm-gather.png", "M8-1-thread-palm.png",
                "M8-1-turn-wrist-before.png", "M8-1-turn-wrist-after.png"
            };
            float beforeWrist = 0f;
            Vector3 beforeWristPosition = Vector3.zero;
            for (int i = 0; i < gestures.Length; i++)
            {
                presenter.PreviewHandGesture(gestures[i], progress[i]);
                if (i == 2)
                {
                    beforeWrist = presenter.LeftWristRotation;
                    beforeWristPosition = presenter.LeftWristPosition;
                }
                if (i == 3)
                {
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(beforeWrist, presenter.LeftWristRotation)),
                        Is.GreaterThan(150f));
                    Assert.That(Vector3.Distance(beforeWristPosition, presenter.LeftWristPosition),
                        Is.LessThan(0.001f));
                }
                camera.Render();
                RenderTexture.active = target;
                capture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
                capture.Apply();
                string output = Path.Combine(projectRoot, "Logs", names[i]);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, capture.EncodeToPNG());
                Assert.That(new FileInfo(output).Length, Is.GreaterThan(10000));
            }

            Assert.That(Vector3.Distance(leftFoot, presenter.LeftAnklePosition), Is.LessThan(0.025f));
            Assert.That(Vector3.Distance(rightFoot, presenter.RightAnklePosition), Is.LessThan(0.025f));
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FormalPalmGesture_RendersFromSuccessfulJudgment()
        {
            var root = new GameObject("M8.1 Formal Palm Gesture Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 48d, PlayDifficulty.Normal), 120d, 48d);
            presenter.Begin(phrases);
            DancePhrase press = phrases.First(p => p.Action == DanceAction.PressPalm);
            presenter.Tick(press.StartSeconds);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                press.AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            presenter.Tick(press.StartSeconds + press.DurationSeconds);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, presenter.LeftWristRotation)), Is.GreaterThan(55f));
            Assert.That(presenter.GetRodDrive(0), Is.GreaterThan(0.2f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FormalThreadPalmIntoTurnWrist_RendersContinuousBoundaryAndVisibleWristFlip()
        {
            var root = new GameObject("M8.1 Formal Thread And Wrist Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Normal), 120d, 64d);
            presenter.Begin(phrases);
            DancePhrase thread = phrases.First(p => p.Action == DanceAction.ThreadPalm);
            DancePhrase turn = phrases.First(p => p.Action == DanceAction.TurnWrist);

            presenter.Tick(thread.StartSeconds);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                thread.AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            presenter.Tick(turn.StartSeconds);
            float boundaryWrist = presenter.LeftWristRotation;
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                turn.AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 2, 2000, 1d));
            presenter.Tick(turn.StartSeconds + 0.0001d);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(boundaryWrist, presenter.LeftWristRotation)),
                Is.LessThan(0.1f));

            presenter.Tick(turn.StartSeconds + (turn.DurationSeconds * 0.8d));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(boundaryWrist, presenter.LeftWristRotation)),
                Is.GreaterThan(70f));
            Assert.That(presenter.GetRodDrive(0), Is.GreaterThan(0.2f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ExperimentalFistPalmSalute_BringsTwoHandsTogetherAndRendersPreview()
        {
            var root = new GameObject("M8.1 Fist Palm Salute Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            Vector3 leftFoot = presenter.LeftAnklePosition;
            Vector3 rightFoot = presenter.RightAnklePosition;
            presenter.PreviewFistPalmSalute(1f);

            Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.RightWristPosition),
                Is.LessThan(0.65f));
            Assert.That(presenter.RightFingerScale.x, Is.LessThan(0.7f));
            Assert.That(Vector3.Distance(leftFoot, presenter.LeftAnklePosition), Is.LessThan(0.025f));
            Assert.That(Vector3.Distance(rightFoot, presenter.RightAnklePosition), Is.LessThan(0.025f));

            var cameraObject = new GameObject("M8.1 Salute Validation Camera");
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
            string output = Path.Combine(projectRoot, "Logs", "M8-1-fist-palm-salute.png");
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

        [Test]
        public void FormalFistPalmSalute_RendersFromSuccessfulJudgmentAndCloses()
        {
            var root = new GameObject("M8.1 Formal Fist Palm Salute Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Normal), 120d, 64d);
            presenter.Begin(phrases);
            DancePhrase salute = phrases.First(p => p.Action == DanceAction.FistPalmSalute);
            presenter.Tick(salute.StartSeconds);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                salute.AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            presenter.Tick(salute.StartSeconds + (salute.DurationSeconds * 0.7d));

            Assert.That(Vector3.Distance(presenter.LeftWristPosition, presenter.RightWristPosition),
                Is.LessThan(0.65f));
            Assert.That(presenter.RightFingerScale.x, Is.LessThan(0.7f));
            Assert.That(presenter.GetRodDrive(0), Is.GreaterThan(0.2f));
            Assert.That(presenter.GetRodDrive(2), Is.GreaterThan(0.2f));

            presenter.Tick(salute.StartSeconds + salute.DurationSeconds);
            Assert.That(presenter.RightFingerScale.x, Is.EqualTo(1f).Within(0.01f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void DanceTurn_LowerBodyFollowsFacing_WhileBothFeetStayGrounded()
        {
            var root = new GameObject("Northern Waist Separation Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            presenter.Begin(phrases);
            DancePhrase turn = phrases[3];
            presenter.Tick(turn.StartSeconds);
            Vector3 left = presenter.LeftAnklePosition;
            Vector3 right = presenter.RightAnklePosition;
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                turn.AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            for (int i = 0; i <= 60; i++)
            {
                presenter.Tick(turn.StartSeconds + (turn.DurationSeconds * i / 60d));
                Assert.That(Vector3.Distance(left, presenter.LeftAnklePosition), Is.LessThan(0.025f));
                Assert.That(Vector3.Distance(right, presenter.RightAnklePosition), Is.LessThan(0.025f));
                Assert.That(presenter.LowerBodyFacingScale,
                    Is.EqualTo(presenter.FacingScale).Within(0.0001f));
            }
            presenter.Tick(turn.StartSeconds + turn.DurationSeconds);
            Assert.That(presenter.FacingScale, Is.LessThan(-0.9f));
            Assert.That(presenter.LowerBodyFacingScale, Is.LessThan(-0.9f));
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Left Hip")
                .localPosition.x, Is.GreaterThan(0.1f));
            Assert.That(root.transform.Find("M6 Shadow Puppet Stage/Joint Pelvis/Joint Right Hip")
                .localPosition.x, Is.LessThan(-0.1f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void StructurePreview_RendersStepAndPlantedFoot()
        {
            var root = new GameObject("Northern Rig Visual Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            presenter.Begin();
            presenter.PreviewStructure(0.55f);
            var cameraObject = new GameObject("Northern Rig Camera");
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
            string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "M8-R-northern-rig-step.png");
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

    public sealed class DanceExplanationHudTests
    {
        [Test]
        public void Hud_UsesPerformedPhraseAndNeverLabelsMissAsCurrentMove()
        {
            var root = new GameObject("Dance HUD Test");
            var hud = root.AddComponent<GameplayHudPresenter>();
            hud.Begin(0, DifficultyConfig.Prototype);
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            Transform panel = root.transform.Find("M4 中文界面/安全区域/演出解说");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponent<UnityEngine.UI.Image>().raycastTarget, Is.False);
            var name = panel.Find("当前动作").GetComponent<UnityEngine.UI.Text>();
            var state = panel.Find("演出状态").GetComponent<UnityEngine.UI.Text>();
            var detail = panel.Find("拍数与控制").GetComponent<UnityEngine.UI.Text>();

            hud.ShowDanceStatus(new DancePerformanceStatus(DancePerformanceKind.Performing, phrases[0]));
            Assert.That(name.text, Is.EqualTo("單山膀"));
            Assert.That(detail.text, Does.Contain("左肩、左肘"));
            hud.ShowDanceStatus(new DancePerformanceStatus(
                DancePerformanceKind.Interrupted, phrases[0], phrases[1]));
            Assert.That(name.text, Does.Contain("單山膀"));
            Assert.That(name.text, Does.Not.Contain("雲手"));
            Assert.That(state.text, Does.Contain("雲手 未演"));
            hud.ShowDanceStatus(new DancePerformanceStatus(DancePerformanceKind.Waiting, null));
            Assert.That(name.text, Is.EqualTo("尚未起势"));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void PuppetPresenter_ForwardsPlaybackStatusAndResetsOnBegin()
        {
            var root = new GameObject("Dance Status Test");
            var presenter = root.AddComponent<ShadowPuppetPresenter>();
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 32d, PlayDifficulty.Normal), 120d, 32d);
            var kinds = new System.Collections.Generic.List<DancePerformanceKind>();
            presenter.DanceStatusChanged += status => kinds.Add(status.Kind);
            presenter.Begin(phrases);
            Assert.That(kinds[kinds.Count - 1], Is.EqualTo(DancePerformanceKind.Waiting));
            presenter.Tick(0d);
            presenter.OnJudged(new JudgmentResult(JudgmentEventKind.NoteJudged,
                phrases[0].AnchorNoteId, 0, JudgmentGrade.Perfect, 0d, 1, 1000, 1d));
            Assert.That(kinds[kinds.Count - 1], Is.EqualTo(DancePerformanceKind.Performing));
            presenter.Begin();
            Assert.That(kinds[kinds.Count - 1], Is.EqualTo(DancePerformanceKind.Waiting));
            Object.DestroyImmediate(root);
        }
    }
}
