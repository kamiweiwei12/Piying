using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2FormalIntegrationTests
    {
        [Test]
        public void P7_SuccessAdvancesV2NamesWhileMissFreezesAndReportsCueAsUnperformed()
        {
            DancePhrase[] timing = Phrases();
            var playback = new PuppetV2Playback(timing);
            var statuses = new List<DancePerformanceStatus>();
            playback.StatusChanged += statuses.Add;

            playback.Evaluate(timing[0].StartSeconds);
            playback.OnJudged(Result(timing[0], JudgmentGrade.Perfect), timing[0].StartSeconds);
            Assert.That(statuses[^1].PerformedName, Is.EqualTo("缓步入场"));
            playback.Evaluate(timing[0].StartSeconds + (timing[0].DurationSeconds * 0.5d));
            PuppetV2Pose moving = playback.CurrentPose;
            Assert.That(moving.RootX, Is.GreaterThan(PuppetV2SequenceChoreography.Evaluate(0d).RootX));

            playback.Evaluate(timing[0].StartSeconds + timing[0].DurationSeconds);
            PuppetV2Pose held = playback.CurrentPose;
            playback.OnJudged(Result(timing[1], JudgmentGrade.Miss), timing[1].StartSeconds);
            playback.Evaluate(timing[1].StartSeconds + timing[1].DurationSeconds);
            Assert.That(playback.CurrentPose.RootX, Is.EqualTo(held.RootX).Within(0.000001d));
            Assert.That(statuses[^1].Kind, Is.EqualTo(DancePerformanceKind.Interrupted));
            Assert.That(statuses[^1].PerformedName, Is.EqualTo("缓步入场"));
            Assert.That(statuses[^1].CueName, Is.EqualTo("整冠肃立"));
        }

        [Test]
        public void P7_PresenterUsesV2PlaybackAndHudShowsOnlyLargeMoveMetadata()
        {
            var puppetRoot = new GameObject("P7 V2 Presenter Test");
            var hudRoot = new GameObject("P7 V2 HUD Test");
            var presenter = puppetRoot.AddComponent<ShadowPuppetPresenter>();
            var hud = hudRoot.AddComponent<GameplayHudPresenter>();
            DancePhrase[] timing = Phrases();
            DancePerformanceStatus latest = default;
            presenter.DanceStatusChanged += status =>
            {
                latest = status;
                hud.ShowDanceStatus(status);
            };
            hud.Begin(0, DifficultyConfig.Prototype);
            presenter.BeginV2(timing);
            presenter.Tick(timing[0].StartSeconds);
            presenter.OnJudged(Result(timing[0], JudgmentGrade.Perfect));

            Assert.That(latest.Kind, Is.EqualTo(DancePerformanceKind.Performing));
            Assert.That(latest.PerformedName, Is.EqualTo("缓步入场"));
            Transform panel = hudRoot.transform.Find("M4 中文界面/安全区域/演出解说");
            Assert.That(panel.Find("当前动作").GetComponent<UnityEngine.UI.Text>().text,
                Is.EqualTo("缓步入场"));
            Assert.That(panel.Find("拍数与控制").GetComponent<UnityEngine.UI.Text>().text,
                Does.Not.Contain("控制"));

            Object.DestroyImmediate(puppetRoot);
            Object.DestroyImmediate(hudRoot);
        }

        [Test]
        public void P7_RendersFormalSuccessAndMissHudEvidence()
        {
            var puppetRoot = new GameObject("P7 V2 Visual Puppet");
            var hudRoot = new GameObject("P7 V2 Visual HUD");
            var cameraRoot = new GameObject("P7 V2 Visual Camera");
            var presenter = puppetRoot.AddComponent<ShadowPuppetPresenter>();
            var hud = hudRoot.AddComponent<GameplayHudPresenter>();
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            hud.Begin(0, DifficultyConfig.Prototype);
            Canvas canvas = hudRoot.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            hud.TickSongTime(1d);
            presenter.DanceStatusChanged += hud.ShowDanceStatus;
            DancePhrase[] timing = Phrases();
            presenter.BeginV2(timing);
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "PuppetV2", "P7-FormalIntegration");
            Directory.CreateDirectory(directory);

            presenter.Tick(timing[0].StartSeconds);
            presenter.OnJudged(Result(timing[0], JudgmentGrade.Perfect));
            presenter.Tick(timing[0].StartSeconds + (timing[0].DurationSeconds * 0.5d));
            SaveFrame(camera, target, capture, Path.Combine(directory, "success-v2-hud.png"));

            presenter.Tick(timing[1].StartSeconds);
            presenter.OnJudged(Result(timing[1], JudgmentGrade.Miss));
            SaveFrame(camera, target, capture, Path.Combine(directory, "miss-v2-hud.png"));

            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraRoot);
            Object.DestroyImmediate(puppetRoot);
            Object.DestroyImmediate(hudRoot);
        }

        private static DancePhrase[] Phrases()
        {
            NoteData[] notes = PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Normal);
            return DanceChoreography.Create(notes, 120d, 64d);
        }

        private static JudgmentResult Result(DancePhrase phrase, JudgmentGrade grade)
        {
            return new JudgmentResult(JudgmentEventKind.NoteJudged,
                phrase.AnchorNoteId, 1, grade, 0d, grade == JudgmentGrade.Miss ? 0 : 1,
                grade == JudgmentGrade.Miss ? 0 : 1000, grade == JudgmentGrade.Miss ? 0d : 1d);
        }

        private static void SaveFrame(Camera camera, RenderTexture target, Texture2D capture, string path)
        {
            camera.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0f, 0f, capture.width, capture.height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(path, capture.EncodeToPNG());
        }
    }
}
