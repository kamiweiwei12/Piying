using System;
using System.Collections.Generic;
using UnityEngine;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Unity.Config
{
    [CreateAssetMenu(menuName = "YingYun/Song Definition", fileName = "SongDefinition")]
    public sealed class SongDefinitionAsset : ScriptableObject, IPlayableSongDefinition
    {
        public enum TimingStatus
        {
            AnalysisCandidate,
            Verified,
        }

        [Serializable]
        public struct TimingPointData
        {
            public double timeSec;
            public double beat;
            public double bpm;
            public int beatsPerBar;
        }

        [Serializable]
        public struct NoteRecord
        {
            public int id;
            public string typeId;
            public int lane;
            public double timeSec;
            public double durationSec;
            public int segmentId;
            public int requiredLanesMask;

            public NoteData ToRuntime()
            {
                return new NoteData(id, typeId, lane, timeSec, durationSec, segmentId, requiredLanesMask);
            }
        }

        [Serializable]
        public sealed class DifficultyChart
        {
            public PlayDifficulty difficulty;
            public NoteRecord[] notes = Array.Empty<NoteRecord>();
        }

        [Serializable]
        public struct DanceCueData
        {
            public DanceAction action;
            public double startBeat;
            public int durationBeats;
            public int anchorNoteId;
            public bool hasClosing;
        }

        [SerializeField] private int dataVersion = 1;
        [SerializeField] private string songId = string.Empty;
        [SerializeField] private string title = string.Empty;
        [SerializeField] private string artist = string.Empty;
        [SerializeField] private AudioClip music;
        [SerializeField] private TimingStatus timingStatus;
        [SerializeField] private double firstPlayableSec;
        [SerializeField] private double playableEndSec;
        [SerializeField] private TimingPointData[] timingPoints = Array.Empty<TimingPointData>();
        [SerializeField] private DifficultyChart[] charts = Array.Empty<DifficultyChart>();
        [SerializeField] private DanceCueData[] danceCues = Array.Empty<DanceCueData>();

        public int DataVersion => dataVersion;
        public string SongId => songId;
        public string Title => title;
        public string Artist => artist;
        public AudioClip Music => music;
        public TimingStatus CurrentTimingStatus => timingStatus;
        public double FirstPlayableSec => firstPlayableSec;
        public double PlayableEndSec => playableEndSec > 0d ? playableEndSec : music != null ? music.length : 0d;
        public bool HasAuthoredCharts => charts != null && charts.Length > 0;

        public SongTimingMap CreateTimingMap()
        {
            var points = new SongTimingPoint[timingPoints.Length];
            for (int i = 0; i < timingPoints.Length; i++)
            {
                TimingPointData point = timingPoints[i];
                points[i] = new SongTimingPoint(point.timeSec, point.beat, point.bpm, point.beatsPerBar);
            }

            return new SongTimingMap(points);
        }

        public NoteData[] GetNotes(PlayDifficulty difficulty)
        {
            for (int i = 0; i < charts.Length; i++)
            {
                if (charts[i].difficulty != difficulty)
                {
                    continue;
                }

                NoteRecord[] records = charts[i].notes ?? Array.Empty<NoteRecord>();
                var result = new NoteData[records.Length];
                for (int noteIndex = 0; noteIndex < records.Length; noteIndex++)
                {
                    result[noteIndex] = records[noteIndex].ToRuntime();
                }

                return result;
            }

            return Array.Empty<NoteData>();
        }

        public AuthoredDanceCue[] GetDanceCues()
        {
            var result = new AuthoredDanceCue[danceCues.Length];
            for (int i = 0; i < danceCues.Length; i++)
            {
                DanceCueData cue = danceCues[i];
                if (Math.Abs(cue.startBeat - Math.Round(cue.startBeat)) > 0.000001d)
                {
                    throw new InvalidOperationException("Dance cue start beats must be whole beats.");
                }

                result[i] = new AuthoredDanceCue(
                    cue.action,
                    (int)Math.Round(cue.startBeat),
                    cue.durationBeats,
                    cue.anchorNoteId,
                    cue.hasClosing);
            }

            return result;
        }

        public void ValidateOrThrow()
        {
            if (dataVersion <= 0 || string.IsNullOrWhiteSpace(songId) || string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("Song identity and data version are required.");
            }

            if (music == null)
            {
                throw new InvalidOperationException("Music clip is required.");
            }

            CreateTimingMap();
            double endSec = PlayableEndSec;
            if (endSec > music.length + 0.001d)
            {
                throw new InvalidOperationException("Playable range cannot exceed the music clip.");
            }

            var playableRange = new SongPlayableRange(firstPlayableSec, endSec);
            var seenDifficulties = new HashSet<PlayDifficulty>();
            for (int i = 0; i < charts.Length; i++)
            {
                DifficultyChart chart = charts[i];
                if (chart == null || !seenDifficulties.Add(chart.difficulty))
                {
                    throw new InvalidOperationException("Each difficulty chart must be unique and non-null.");
                }

                NoteData[] runtimeNotes = GetNotes(chart.difficulty);
                SongChartValidation.ValidateNotes(
                    runtimeNotes,
                    music.length,
                    playableRange);
                SongChartValidation.ValidateDifficultyFeatures(runtimeNotes, chart.difficulty);
            }

            for (int i = 0; i < danceCues.Length; i++)
            {
                if (danceCues[i].durationBeats < 4 || danceCues[i].startBeat < 0d)
                {
                    throw new InvalidOperationException("Dance cues require a non-negative start and at least four beats.");
                }
            }


            AuthoredDanceCue[] authoredCues = GetDanceCues();
            SongTimingMap timingMap = CreateTimingMap();
            for (int i = 0; i < charts.Length; i++)
            {
                DancePhrase[] phrases = DanceChoreography.CreateAuthored(
                    GetNotes(charts[i].difficulty),
                    timingMap,
                    authoredCues);
                _ = new DancePlayback(phrases);
                if (phrases.Length > 0 &&
                    phrases[phrases.Length - 1].StartSeconds + phrases[phrases.Length - 1].DurationSeconds > endSec + 0.001d)
                {
                    throw new InvalidOperationException("Dance cues must remain inside the playable range.");
                }
            }
        }

#if UNITY_EDITOR
        public void ConfigureEditor(
            string id,
            string displayTitle,
            string displayArtist,
            AudioClip clip,
            double startSec,
            double endSec,
            TimingPointData[] points,
            TimingStatus status = TimingStatus.AnalysisCandidate)
        {
            songId = id;
            title = displayTitle;
            artist = displayArtist;
            music = clip;
            timingStatus = status;
            firstPlayableSec = startSec;
            playableEndSec = endSec;
            timingPoints = points ?? Array.Empty<TimingPointData>();
            charts = Array.Empty<DifficultyChart>();
            danceCues = Array.Empty<DanceCueData>();
        }


        public void ConfigureAuthoredContentEditor(
            DifficultyChart[] difficultyCharts,
            DanceCueData[] cues,
            TimingStatus status)
        {
            charts = difficultyCharts ?? Array.Empty<DifficultyChart>();
            danceCues = cues ?? Array.Empty<DanceCueData>();
            timingStatus = status;
        }
#endif
    }
}
