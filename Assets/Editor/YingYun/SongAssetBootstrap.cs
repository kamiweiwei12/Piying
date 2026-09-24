using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using YingYun.Rhythm.Unity.Config;

namespace YingYun.Rhythm.Editor
{
    public static class SongAssetBootstrap
    {
        private const string SongDirectory = "Assets/Resources/YingYun/Songs";
        private const string CatalogPath = "Assets/Resources/YingYun/SongCatalog.asset";
        private const string XiangWangXingBeats = "Tools/BeatAnalysis/results/xiang-wang-xing.beats";
        private const string QingYuAnBeats = "Tools/BeatAnalysis/results/qing-yu-an-lan-jie.beats";

        public static void CreateInitialSongAssets()
        {
            ConfigureStreamingMusic("Assets/Audio/Music/象王行（特别版）.mp3");
            ConfigureStreamingMusic("Assets/Audio/Music/青玉案·兰芥.mp3");
            EnsureAssetFolder("Assets/Resources");
            EnsureAssetFolder("Assets/Resources/YingYun");
            EnsureAssetFolder(SongDirectory);

            SongDefinitionAsset trialLight = CreateOrUpdate(
                "TrialLight",
                "trial-light",
                "试灯",
                "YingYun Prototype",
                "Assets/Audio/Music.wav",
                0d,
                CreateFixedTiming(0d, 120d),
                SongDefinitionAsset.TimingStatus.Verified);

            SongDefinitionAsset.TimingPointData[] xiangTiming = LoadBeatThisTiming(XiangWangXingBeats);

            SongDefinitionAsset xiangWangXing = CreateOrUpdate(
                "XiangWangXingSpecial",
                "xiang-wang-xing-special",
                "象王行（特别版）",
                "关大洲",
                "Assets/Audio/Music/象王行（特别版）.mp3",
                xiangTiming[0].timeSec,
                xiangTiming,
                SongDefinitionAsset.TimingStatus.AnalysisCandidate);
            XiangWangXingChartAuthoring.Apply(xiangWangXing);

            SongDefinitionAsset.TimingPointData[] qingTiming = LoadBeatThisTiming(QingYuAnBeats);

            SongDefinitionAsset qingYuAn = CreateOrUpdate(
                "QingYuAnLanJie",
                "qing-yu-an-lan-jie",
                "青玉案·兰芥",
                "本地音频标签：Andree Son",
                "Assets/Audio/Music/青玉案·兰芥.mp3",
                qingTiming[0].timeSec,
                qingTiming,
                SongDefinitionAsset.TimingStatus.AnalysisCandidate);
            QingYuAnLanJieChartAuthoring.Apply(qingYuAn);

            SongCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<SongCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SongCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.ConfigureEditor(new[] { trialLight, xiangWangXing, qingYuAn });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created initial song catalog at {CatalogPath}");
        }

        private static SongDefinitionAsset CreateOrUpdate(
            string assetName,
            string songId,
            string title,
            string artist,
            string audioPath,
            double firstPlayableSec,
            SongDefinitionAsset.TimingPointData[] timingPoints,
            SongDefinitionAsset.TimingStatus status)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Audio clip was not found: {audioPath}");
            }

            string assetPath = Path.Combine(SongDirectory, assetName + ".asset").Replace('\\', '/');
            SongDefinitionAsset definition = AssetDatabase.LoadAssetAtPath<SongDefinitionAsset>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<SongDefinitionAsset>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.ConfigureEditor(
                songId,
                title,
                artist,
                clip,
                firstPlayableSec,
                clip.length,
                timingPoints,
                status);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static SongDefinitionAsset.TimingPointData[] CreateFixedTiming(double timeSec, double bpm)
        {
            return new[]
            {
                new SongDefinitionAsset.TimingPointData
                {
                    timeSec = timeSec,
                    beat = 0d,
                    bpm = bpm,
                    beatsPerBar = 4,
                },
            };
        }

        private static SongDefinitionAsset.TimingPointData[] LoadBeatThisTiming(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string fullPath = Path.Combine(projectRoot ?? string.Empty, projectRelativePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Beat This result was not found.", fullPath);
            }

            var times = new List<double>();
            var barBeats = new List<int>();
            foreach (string line in File.ReadAllLines(fullPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] fields = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length != 2 ||
                    !double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double timeSec) ||
                    !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int barBeat) ||
                    barBeat < 1 || barBeat > 16)
                {
                    throw new FormatException($"Invalid Beat This row: {line}");
                }

                if (times.Count > 0 && timeSec <= times[times.Count - 1])
                {
                    throw new FormatException("Beat This timestamps must be strictly increasing.");
                }

                if (barBeats.Count > 0 && barBeat != 1 && barBeat != barBeats[barBeats.Count - 1] + 1)
                {
                    throw new FormatException("Beat-in-bar values must increment or restart at one.");
                }

                times.Add(timeSec);
                barBeats.Add(barBeat);
            }

            if (times.Count < 2 || barBeats[0] != 1)
            {
                throw new FormatException("Beat This data requires at least two beats and must start at a downbeat.");
            }

            var result = new SongDefinitionAsset.TimingPointData[times.Count];
            int barStart = 0;
            int previousCompleteMeter = 4;
            while (barStart < barBeats.Count)
            {
                int nextBar = barStart + 1;
                while (nextBar < barBeats.Count && barBeats[nextBar] != 1)
                {
                    nextBar++;
                }

                int detectedMeter = barBeats[nextBar - 1];
                if (nextBar == barBeats.Count && detectedMeter < 2)
                {
                    detectedMeter = previousCompleteMeter;
                }
                else
                {
                    previousCompleteMeter = detectedMeter;
                }

                for (int i = barStart; i < nextBar; i++)
                {
                    double interval = i + 1 < times.Count
                        ? times[i + 1] - times[i]
                        : times[i] - times[i - 1];
                    result[i] = new SongDefinitionAsset.TimingPointData
                    {
                        timeSec = times[i],
                        beat = i,
                        bpm = 60d / interval,
                        beatsPerBar = detectedMeter,
                    };
                }

                barStart = nextBar;
            }

            return result;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Invalid asset folder: {path}");
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ConfigureStreamingMusic(string assetPath)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Audio importer was not found: {assetPath}");
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.Streaming && !settings.preloadAudioData)
            {
                return;
            }

            settings.loadType = AudioClipLoadType.Streaming;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
