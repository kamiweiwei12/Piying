using System;
using System.Collections.Generic;
using UnityEngine;

namespace YingYun.Rhythm.Unity.Config
{
    [CreateAssetMenu(menuName = "YingYun/Song Catalog", fileName = "SongCatalog")]
    public sealed class SongCatalogAsset : ScriptableObject
    {
        [SerializeField] private SongDefinitionAsset[] songs = Array.Empty<SongDefinitionAsset>();

        public IReadOnlyList<SongDefinitionAsset> Songs => songs;

        public SongDefinitionAsset Find(string songId)
        {
            for (int i = 0; i < songs.Length; i++)
            {
                SongDefinitionAsset song = songs[i];
                if (song != null && string.Equals(song.SongId, songId, StringComparison.Ordinal))
                {
                    return song;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void ConfigureEditor(SongDefinitionAsset[] definitions)
        {
            songs = definitions ?? Array.Empty<SongDefinitionAsset>();
        }
#endif
    }
}
