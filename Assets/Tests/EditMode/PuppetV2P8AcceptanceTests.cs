using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Prototype;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Puppet.V2;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Unity.Config;
using YingYun.Rhythm.View;
using Object = UnityEngine.Object;

namespace YingYun.Rhythm.Tests
{
    public sealed class PuppetV2P8AcceptanceTests
    {
        [Test]
        public void P8_BothBuiltInSongsAndAllDifficultiesDriveV2WithoutChangingCharts()
        {
            SongCatalogAsset catalog = Resources.Load<SongCatalogAsset>("YingYun/SongCatalog");
            foreach (string songId in new[]
            {
                RhythmPrototypeController.XiangWangXingSongId,
                RhythmPrototypeController.QingYuAnLanJieSongId
            })
            {
                SongDefinitionAsset song = catalog.Find(songId);
                foreach (PlayDifficulty difficulty in Enum.GetValues(typeof(PlayDifficulty)))
                {
                    NoteData[] notes = song.GetNotes(difficulty);
                    int noteCount = notes.Length;
                    DancePhrase[] phrases = DanceChoreography.CreateAuthored(
                        notes, song.CreateTimingMap(), song.GetDanceCues());
                    var playback = new PuppetV2Playback(phrases);
                    var names = new List<string>();
                    playback.StatusChanged += status =>
                    {
                        if (status.Kind == DancePerformanceKind.Performing) names.Add(status.PerformedName);
                    };
                    int sampleCount = Math.Min(PuppetV2SequenceChoreography.Count, phrases.Length);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        playback.Evaluate(phrases[i].StartSeconds);
                        playback.OnJudged(Result(phrases[i], JudgmentGrade.Perfect), phrases[i].StartSeconds);
                        playback.Evaluate(phrases[i].StartSeconds + phrases[i].DurationSeconds);
                    }
                    Assert.That(notes.Length, Is.EqualTo(noteCount), $"{songId} {difficulty}");
                    Assert.That(names.Count, Is.EqualTo(sampleCount), $"{songId} {difficulty}");
                    for (int i = 0; i < sampleCount; i++)
                        Assert.That(names[i], Is.EqualTo(PuppetV2SequenceChoreography.GetName(i)));
                    Assert.That(double.IsNaN(playback.CurrentPose.RootX), Is.False);
                }
            }
        }

        [Test]
        public void P8_CustomPathPauseRestartMissAndFrameRatesRemainDeterministic()
        {
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Hard), 120d, 64d);
            PuppetV2Pose atThirty = SampleAtRate(phrases, 30, 2.75d);
            PuppetV2Pose atSixty = SampleAtRate(phrases, 60, 2.75d);
            PuppetV2Pose atOneTwenty = SampleAtRate(phrases, 120, 2.75d);
            AssertPoseEqual(atThirty, atSixty);
            AssertPoseEqual(atThirty, atOneTwenty);

            var playback = new PuppetV2Playback(phrases);
            playback.Evaluate(phrases[0].StartSeconds);
            playback.OnJudged(Result(phrases[0], JudgmentGrade.Perfect), phrases[0].StartSeconds);
            playback.Evaluate(phrases[0].StartSeconds + 1d);
            PuppetV2Pose paused = playback.CurrentPose;
            AssertPoseEqual(paused, playback.CurrentPose);
            playback.OnJudged(Result(phrases[1], JudgmentGrade.Miss), phrases[1].StartSeconds);
            PuppetV2Pose heldAfterMiss = playback.CurrentPose;
            playback.Evaluate(phrases[1].StartSeconds + phrases[1].DurationSeconds);
            AssertPoseEqual(heldAfterMiss, playback.CurrentPose);

            var restarted = new PuppetV2Playback(phrases);
            restarted.Evaluate(phrases[0].StartSeconds);
            restarted.OnJudged(Result(phrases[0], JudgmentGrade.Perfect), phrases[0].StartSeconds);
            restarted.Evaluate(phrases[0].StartSeconds + 1d);
            AssertPoseEqual(paused, restarted.CurrentPose);
        }

        [Test]
        public void P8_RendersHudAtSixteenNineFourThreeAndUltrawide()
        {
            var puppetRoot = new GameObject("P8 Aspect Puppet");
            var hudRoot = new GameObject("P8 Aspect HUD");
            var cameraRoot = new GameObject("P8 Aspect Camera");
            var presenter = puppetRoot.AddComponent<ShadowPuppetPresenter>();
            var hud = hudRoot.AddComponent<GameplayHudPresenter>();
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 3.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.012f, 0.009f, 1f);
            hud.Begin(0, DifficultyConfig.Prototype);
            hud.TickSongTime(1d);
            Canvas canvas = hudRoot.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            presenter.DanceStatusChanged += hud.ShowDanceStatus;
            DancePhrase[] phrases = DanceChoreography.Create(
                PrototypeDanceChart.Create(120d, 64d, PlayDifficulty.Normal), 120d, 64d);
            presenter.BeginV2(phrases);
            presenter.Tick(phrases[0].StartSeconds);
            presenter.OnJudged(Result(phrases[0], JudgmentGrade.Perfect));
            presenter.Tick(phrases[0].StartSeconds + (phrases[0].DurationSeconds * 0.5d));
            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Logs", "PuppetV2", "P8-Acceptance");
            Directory.CreateDirectory(directory);
            Render(camera, 1280, 720, Path.Combine(directory, "hud-16x9.png"));
            Render(camera, 960, 720, Path.Combine(directory, "hud-4x3.png"));
            Render(camera, 1680, 720, Path.Combine(directory, "hud-21x9.png"));

            Object.DestroyImmediate(cameraRoot);
            Object.DestroyImmediate(puppetRoot);
            Object.DestroyImmediate(hudRoot);
        }

        private static PuppetV2Pose SampleAtRate(DancePhrase[] phrases, int fps, double offset)
        {
            var playback = new PuppetV2Playback(phrases);
            playback.Evaluate(phrases[0].StartSeconds);
            playback.OnJudged(Result(phrases[0], JudgmentGrade.Perfect), phrases[0].StartSeconds);
            int frames = (int)Math.Ceiling(offset * fps);
            for (int i = 1; i <= frames; i++)
                playback.Evaluate(phrases[0].StartSeconds + Math.Min(offset, i / (double)fps));
            return playback.CurrentPose;
        }

        private static JudgmentResult Result(DancePhrase phrase, JudgmentGrade grade)
        {
            return new JudgmentResult(JudgmentEventKind.NoteJudged,
                phrase.AnchorNoteId, 1, grade, 0d, grade == JudgmentGrade.Miss ? 0 : 1,
                grade == JudgmentGrade.Miss ? 0 : 1000, grade == JudgmentGrade.Miss ? 0d : 1d);
        }

        private static void AssertPoseEqual(PuppetV2Pose a, PuppetV2Pose b)
        {
            Assert.That(a.RootX, Is.EqualTo(b.RootX).Within(0.000001d));
            Assert.That(a.RootY, Is.EqualTo(b.RootY).Within(0.000001d));
            Assert.That(a.LeftHandX, Is.EqualTo(b.LeftHandX).Within(0.000001d));
            Assert.That(a.RightHandX, Is.EqualTo(b.RightHandX).Within(0.000001d));
            Assert.That(a.LeftFootX, Is.EqualTo(b.LeftFootX).Within(0.000001d));
            Assert.That(a.RightFootX, Is.EqualTo(b.RightFootX).Within(0.000001d));
            Assert.That(a.Facing, Is.EqualTo(b.Facing).Within(0.000001d));
        }

        private static void Render(Camera camera, int width, int height, string path)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var capture = new Texture2D(width, height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            capture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(path, capture.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(capture);
            Object.DestroyImmediate(target);
        }
    }
}
