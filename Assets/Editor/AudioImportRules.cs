using UnityEditor;
using UnityEngine;

// Import settings for every clip under Assets/Audio/Clips, by folder, so no clip is
// tuned by hand (SoundSystemPlan.md §3.8):
//   SFX/Loops/**       mono, Compressed In Memory, Vorbis 60 %   (fire, torch, burning, slide)
//   SFX/**, UI/**      mono, Decompress On Load,   Vorbis 70 %   (instant start, no decode hitch)
//   Ambience/Beds/**   stereo, Streaming,          Vorbis 55 %   (wind beds)
//   Ambience/**        mono, Compressed In Memory, Vorbis 60 %   (gusts, birds, howls)
//   Music/**           stereo, Streaming,          Vorbis 60 %, loads in the background
// Anything else under Clips (placeholders, ...) is treated as SFX.
// Assets/Audio/_Incoming/ (downloads waiting for the Sound Import window, not in git) is
// imported uncompressed and unchanged, so the window can read its samples.
public class AudioImportRules : AssetPostprocessor
{
    public const string ClipsRoot = "Assets/Audio/Clips/";
    public const string IncomingRoot = "Assets/Audio/_Incoming/";

    struct Rule
    {
        public bool mono, background;
        public AudioClipLoadType load;
        public float quality;
        public Rule(bool mono, AudioClipLoadType load, float quality, bool background = false)
        { this.mono = mono; this.load = load; this.quality = quality; this.background = background; }
    }

    static Rule RuleFor(string path)
    {
        string p = path.Substring(ClipsRoot.Length);
        if (p.StartsWith("SFX/Loops/")) return new Rule(true, AudioClipLoadType.CompressedInMemory, 0.6f);
        if (p.StartsWith("Ambience/Beds/")) return new Rule(false, AudioClipLoadType.Streaming, 0.55f);
        if (p.StartsWith("Ambience/")) return new Rule(true, AudioClipLoadType.CompressedInMemory, 0.6f);
        if (p.StartsWith("Music/")) return new Rule(false, AudioClipLoadType.Streaming, 0.6f, background: true);
        return new Rule(true, AudioClipLoadType.DecompressOnLoad, 0.7f);
    }

    void OnPreprocessAudio()
    {
        string path = assetPath.Replace('\\', '/');
        var importer = (AudioImporter)assetImporter;
        if (path.StartsWith(IncomingRoot))
        {
            importer.forceToMono = false;
            AudioImporterSampleSettings raw = importer.defaultSampleSettings;
            raw.loadType = AudioClipLoadType.DecompressOnLoad;
            raw.compressionFormat = AudioCompressionFormat.PCM;
            raw.preloadAudioData = true;
            importer.defaultSampleSettings = raw;
            return;
        }
        if (!path.StartsWith(ClipsRoot)) return;

        Rule rule = RuleFor(path);
        importer.forceToMono = rule.mono;
        importer.loadInBackground = rule.background;

        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.loadType = rule.load;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = rule.quality;
        s.preloadAudioData = rule.load != AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = s;
    }
}
