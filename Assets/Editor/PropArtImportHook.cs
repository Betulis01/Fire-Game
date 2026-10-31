using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

// Adjusts the model prefab the Aseprite importer builds for a prop's art (see
// PropArt), on every import, through the importer's own post-import event -- so the
// fix-up is part of the import and never needs re-applying:
//   - every layer rests on frame 0. The importer gives a layer's renderer its first
//     cel, so a layer drawn only in a tag (an Effect burst on Hit's frames) would show
//     that tag's first frame at rest. A layer with no frame-0 cel rests with no sprite
//     -- not a disabled renderer, since clips key only the sprite. The Animator's write
//     defaults restore this after every clip.
//   - the Highlight layer's renderer starts off. The importer shows hidden layers, and
//     the outline is TargetHighlight's to show (this also keeps it out of the Scene view).
// Prop art is any Individual Layers file under PropFolders.
class PropArtImportHook : AssetPostprocessor
{
    static readonly string[] PropFolders = { "Assets/Sprites/Ground/", "Assets/Sprites/Materials/" };
    static readonly Regex LayerFrame = new(@"^(.+)_Frame_\d+$");

    public override uint GetVersion() => 3;   // bump to reimport every prop file

    void OnPreprocessAsset()
    {
        if (assetImporter is not AsepriteImporter importer) return;
        if (importer.layerImportMode != LayerImportModes.IndividualLayers) return;
        if (!System.Array.Exists(PropFolders, f => assetPath.StartsWith(f))) return;
        importer.OnPostAsepriteImport += OnPostImport;
    }

    static void OnPostImport(AsepriteImporter.ImportEventArgs args)
    {
        var objects = new List<Object>();
        args.context.GetObjects(objects);
        GameObject model = objects.Find(o => o is GameObject go && go.transform.parent == null) as GameObject;
        if (model == null) return;   // no model prefab (generateModelPrefab off)

        // sprites are named "<Layer>_Frame_<frame>"
        var sprites = new Dictionary<string, Sprite>();
        foreach (Object o in objects)
            if (o is Sprite s) sprites[s.name] = s;

        foreach (SpriteRenderer sr in model.GetComponentsInChildren<SpriteRenderer>(true))
        {
            // the layer's name from its first cel (a one-layer file's renderer is the "Root")
            Match m = sr.sprite != null ? LayerFrame.Match(sr.sprite.name) : Match.Empty;
            if (m.Success) sr.sprite = sprites.GetValueOrDefault(m.Groups[1].Value + "_Frame_0");

            if (TargetHighlight.IsHighlight(sr)) sr.enabled = false;
        }
    }
}
