using System.Linq;
using UnityEditor;

// Keeps the Ground Detail tiles in step with their art: whenever an .aseprite in the
// Ground Detail folder is imported (saved from Aseprite), added, moved or deleted,
// GroundDetailSetup.Rebuild re-bakes the tiles' masks and lays out the palette again.
// Runs once the import has finished (delayCall), and once per batch; Rebuild's own
// import-setting fixes reimport the file, which runs it once more and changes nothing.
class GroundDetailImport : AssetPostprocessor
{
    static bool scheduled;

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (scheduled) return;
        if (!imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsSource)) return;

        scheduled = true;
        EditorApplication.delayCall += () =>
        {
            scheduled = false;
            GroundDetailSetup.Rebuild();
        };
    }

    static bool IsSource(string path) =>
        path.EndsWith(".aseprite") && path.StartsWith(GroundDetailSetup.SourceFolder + "/") &&
        path.LastIndexOf('/') == GroundDetailSetup.SourceFolder.Length;
}
