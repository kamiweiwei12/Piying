using System;
using UnityEngine;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;
using YingYun.Rhythm.Unity.Config;

namespace YingYun.Rhythm.Unity.CustomSongs
{
    public sealed class RuntimeSongDefinition : IPlayableSongDefinition
    {
        private readonly SongTimingPoint[] _timingPoints;
        private readonly ProceduralSongContent _content;

        public RuntimeSongDefinition(
            string songId,
            string title,
            string artist,
            AudioClip music,
            SongTimingPoint[] timingPoints,
            ProceduralSongContent content)
        {
            SongId = string.IsNullOrWhiteSpace(songId) ? throw new ArgumentException(nameof(songId)) : songId;
            Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException(nameof(title)) : title;
            Artist = artist ?? string.Empty;
            Music = music != null ? music : throw new ArgumentNullException(nameof(music));
            _timingPoints = timingPoints ?? throw new ArgumentNullException(nameof(timingPoints));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            FirstPlayableSec = _timingPoints[0].TimeSec;
            PlayableEndSec = music.length;
            ValidateOrThrow();
        }

        public string SongId { get; }
        public string Title { get; }
        public string Artist { get; }
        public AudioClip Music { get; }
        public double FirstPlayableSec { get; }
        public double PlayableEndSec { get; }
        public bool HasAuthoredCharts => true;

        public SongTimingMap CreateTimingMap() => new SongTimingMap(_timingPoints);
        public NoteData[] GetNotes(PlayDifficulty difficulty) => _content.GetNotes(difficulty);
        public AuthoredDanceCue[] GetDanceCues() => _content.DanceCues;

        public void ValidateOrThrow()
        {
            SongTimingMap timing = CreateTimingMap();
            var range = new SongPlayableRange(FirstPlayableSec, PlayableEndSec);
            foreach (PlayDifficulty difficulty in Enum.GetValues(typeof(PlayDifficulty)))
            {
                NoteData[] notes = GetNotes(difficulty);
                SongChartValidation.ValidateNotes(notes, Music.length, range);
                SongChartValidation.ValidateDifficultyFeatures(notes, difficulty, timing);
                DancePhrase[] phrases = DanceChoreography.CreateAuthored(notes, timing, GetDanceCues());
                if (phrases.Length > 0 &&
                    phrases[phrases.Length - 1].StartSeconds + phrases[phrases.Length - 1].DurationSeconds > PlayableEndSec + 0.001d)
                {
                    throw new InvalidOperationException("Generated dance cues exceed the audio clip.");
                }
            }
        }
    }
}
