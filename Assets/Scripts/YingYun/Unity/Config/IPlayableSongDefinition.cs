using UnityEngine;
using YingYun.Rhythm.Chart;
using YingYun.Rhythm.Judgment;
using YingYun.Rhythm.Puppet;
using YingYun.Rhythm.Scoring;

namespace YingYun.Rhythm.Unity.Config
{
    public interface IPlayableSongDefinition
    {
        string SongId { get; }
        string Title { get; }
        string Artist { get; }
        AudioClip Music { get; }
        double FirstPlayableSec { get; }
        double PlayableEndSec { get; }
        bool HasAuthoredCharts { get; }
        SongTimingMap CreateTimingMap();
        NoteData[] GetNotes(PlayDifficulty difficulty);
        AuthoredDanceCue[] GetDanceCues();
        void ValidateOrThrow();
    }
}
