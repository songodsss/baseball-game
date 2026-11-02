using UnityEditor;
using UnityEngine;

namespace BaseballGame.EditorTools
{
    /// <summary>
    /// Import options for Resources/Sounds/: songs stream from disk so they don't sit in memory,
    /// short effects are decoded up front so they play the moment a button is pressed.
    /// </summary>
    public class AudioImport : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        const string SoundFolder = "Assets/Resources/Sounds/";
        const string MusicFolder = SoundFolder + "Music/";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(SoundFolder)) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            if (assetPath.StartsWith(MusicFolder))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = 0.6f;
                importer.forceToMono = false;
            }
            else if (assetPath.EndsWith("_loop.ogg"))
            {
                // The crowd under a match: long, but it plays the whole game.
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.quality = 0.5f;
                importer.forceToMono = false;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = 0.7f;
                importer.forceToMono = true;
            }
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = assetPath.StartsWith(MusicFolder);
        }
    }
}
