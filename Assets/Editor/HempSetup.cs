using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

// Builds the hemp chain from its Aseprite art (Tools/Items/Set Up Hemp), linking the
// art by name like PropArtConverter -- the importer's fileIDs are never hand-written:
//   - Hemp (stacks to 8) and Burlap (stacks to 4): an ItemDefinition each, and a bag
//     item prefab copied from Leather with the Art swapped for the file's model
//   - Hemp Plant: copied from the Blue Berries bush (Y-sort, sorting group, bump area)
//     without its berry respawner, with the plant's model as Art (body "Stem"), its own
//     copy of the bush's controller pointed at the plant's clips (the collide tag), a
//     Harvestable that gives Hemp, and an InteractArea copied from Leather
//   - the Recipe Book's "Burlap" recipe: 4 Hemp at a Workbench
// New files arrive with the project's default Aseprite settings (Merge Frame, 16 PPU),
// so each is first set to import as prop art: Individual Layers with hidden layers (the
// Highlight), a model prefab, at the game's 32 PPU.
// Run once after Unity has imported the three files. Anything that already exists is
// skipped, so a re-run only fills in what's missing and never overwrites your tweaks.
static class HempSetup
{
    const string Materials = "Assets/Prefabs/Entities/Materials/";
    const string ItemTemplate = Materials + "Leather.prefab";
    const string PlantFolder = "Assets/Prefabs/Entities/Plants";
    const string PlantPrefab = PlantFolder + "/Hemp Plant.prefab";
    const string PlantTemplate = "Assets/Prefabs/Entities/Food/Red Berries/Blue Berries.prefab";
    const string ControllerFolder = "Assets/Animation/Hemp Plant";
    const string Controller = ControllerFolder + "/Hemp Plant Animator Controller.controller";
    const string ControllerTemplate = "Assets/Animation/Blue Berries/Blue Berries Animator Controller.controller";
    const string RecipeBookPath = "Assets/Prefabs/Recipe Book.asset";

    const string HempArt = "Assets/Sprites/Materials/Hemp.aseprite";
    const string BurlapArt = "Assets/Sprites/Materials/Burlap.aseprite";
    const string PlantArt = "Assets/Sprites/Ground/Fauna/Hemp Plant.aseprite";
    const string PlantBody = "Stem";
    const string InteractArea = "InteractArea";
    const float PixelsPerUnit = 32f;

    [MenuItem("Tools/Items/Set Up Hemp")]
    static void SetUp()
    {
        var log = new List<string>();
        ItemDefinition hemp = Item("Hemp", HempArt, 8, log);
        ItemDefinition burlap = Item("Burlap", BurlapArt, 4, log);
        if (hemp != null) Plant(hemp, log);
        if (hemp != null && burlap != null) BurlapRecipe(hemp, burlap, log);
        AssetDatabase.SaveAssets();
        Debug.Log("[HempSetup] " + string.Join("\n", log));
    }

    // The item's definition and its bag prefab; null when its art can't be used.
    static ItemDefinition Item(string name, string aseprite, int maxStack, List<string> log)
    {
        string defPath = Materials + name + ".asset";
        string prefabPath = Materials + name + ".prefab";

        var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(defPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.id = name;
            def.displayName = name;
            def.size = ItemSize.Small;
            AssetDatabase.CreateAsset(def, defPath);
            log.Add($"Created {defPath}");
        }
        else log.Add($"{defPath} exists; kept");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            GameObject model = Model(aseprite);
            if (model == null) return null;
            if (!AssetDatabase.CopyAsset(ItemTemplate, prefabPath))
            {
                Debug.LogError($"[HempSetup] Couldn't copy {ItemTemplate} to {prefabPath}.");
                return null;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                root.name = name;
                ReplaceArt(root, model);
                root.GetComponent<WorldItem>().item = def;
                root.GetComponent<Pickupable>().promptText = name;
                root.GetComponent<Stackable>().maxStack = maxStack;
                SetPropArt(root, "Layer 1", null);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            log.Add($"Created {prefabPath} (stacks to {maxStack})");
        }
        else log.Add($"{prefabPath} exists; kept");

        if (def.prefab == null)
        {
            def.prefab = prefab;
            EditorUtility.SetDirty(def);
        }
        return def;
    }

    static void Plant(ItemDefinition hemp, List<string> log)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlantPrefab) != null)
        {
            log.Add($"{PlantPrefab} exists; kept");
            return;
        }
        GameObject model = Model(PlantArt);
        if (model == null) return;

        AnimatorController controller = PlantController(log);

        if (!AssetDatabase.IsValidFolder(PlantFolder)) AssetDatabase.CreateFolder("Assets/Prefabs/Entities", "Plants");
        if (!AssetDatabase.CopyAsset(PlantTemplate, PlantPrefab))
        {
            Debug.LogError($"[HempSetup] Couldn't copy {PlantTemplate} to {PlantPrefab}.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PlantPrefab);
        try
        {
            root.name = "Hemp Plant";
            if (root.TryGetComponent(out BerryRespawner respawner)) Object.DestroyImmediate(respawner);
            ReplaceArt(root, model);
            SetPropArt(root, PlantBody, controller);

            var harvest = root.AddComponent<Harvestable>();
            harvest.item = hemp;
            harvest.promptText = "Hemp";
            harvest.promptYOffset = Top(model) + 0.1f;

            // the trigger PlayerInteractor finds interactables by (the Interactable layer)
            Transform area = AssetDatabase.LoadAssetAtPath<GameObject>(ItemTemplate).transform.Find(InteractArea);
            if (area != null)
            {
                GameObject copy = Object.Instantiate(area.gameObject, root.transform, false);
                copy.name = InteractArea;
            }
            else Debug.LogWarning($"[HempSetup] {ItemTemplate} has no {InteractArea}; add one to the plant by hand.");

            PrefabUtility.SaveAsPrefabAsset(root, PlantPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        log.Add($"Created {PlantPrefab}");
    }

    // The bush's controller (idle, plus collide while "Colliding"), copied and pointed at
    // the plant's own clips by tag name.
    static AnimatorController PlantController(List<string> log)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        if (controller == null)
        {
            if (!AssetDatabase.IsValidFolder(ControllerFolder)) AssetDatabase.CreateFolder("Assets/Animation", "Hemp Plant");
            if (!AssetDatabase.CopyAsset(ControllerTemplate, Controller))
            {
                Debug.LogError($"[HempSetup] Couldn't copy {ControllerTemplate} to {Controller}.");
                return null;
            }
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            log.Add($"Created {Controller}");
        }
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(PlantArt).OfType<AnimationClip>().ToArray();
        PropArtConverter.LinkStates(controller, clips, PlantArt);
        return controller;
    }

    static void BurlapRecipe(ItemDefinition hemp, ItemDefinition burlap, List<string> log)
    {
        var book = AssetDatabase.LoadAssetAtPath<RecipeBook>(RecipeBookPath);
        if (book == null)
        {
            Debug.LogError($"[HempSetup] No Recipe Book at {RecipeBookPath}.");
            return;
        }
        var recipes = new List<Recipe>(book.recipes ?? new Recipe[0]);
        if (recipes.Exists(r => r != null && r.output == burlap))
        {
            log.Add("Burlap recipe exists; kept");
            return;
        }
        recipes.Add(new Recipe
        {
            displayName = "Burlap",
            ingredients = new[] { new Ingredient { item = hemp, amount = 4 } },
            output = burlap,
            requiredStation = StationType.Workbench,
            kind = RecipeKind.Instant,
        });
        book.recipes = recipes.ToArray();
        EditorUtility.SetDirty(book);
        log.Add("Added the Burlap recipe (4 Hemp, Workbench) to the Recipe Book");
    }

    // The file's model prefab, imported the way prop art must be; null (logged) otherwise.
    static GameObject Model(string aseprite)
    {
        Configure(aseprite);
        if (!PropArtConverter.PrepareImport(aseprite)) return null;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(aseprite);
        if (model == null) Debug.LogError($"[HempSetup] {aseprite} has no model prefab (Generate Model Prefab off?).");
        return model;
    }

    // Import as prop art (see PropArt / PropArtImportHook); reimports only on a change.
    static void Configure(string aseprite)
    {
        if (AssetImporter.GetAtPath(aseprite) is not AsepriteImporter importer) return;
        bool changed = false;
        if (importer.layerImportMode != LayerImportModes.IndividualLayers)
        {
            importer.layerImportMode = LayerImportModes.IndividualLayers;
            changed = true;
        }
        if (!importer.includeHiddenLayers)
        {
            importer.includeHiddenLayers = true;
            changed = true;
        }
        if (!importer.generateModelPrefab)
        {
            importer.generateModelPrefab = true;
            changed = true;
        }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
        {
            importer.spritePixelsPerUnit = PixelsPerUnit;
            changed = true;
        }
        if (!changed) return;
        EditorUtility.SetDirty(importer);   // the setters don't, and SaveAndReimport only saves a dirty importer
        importer.SaveAndReimport();
    }

    // The template's Art out, the file's model in as the "Art" child (see PropArt).
    static void ReplaceArt(GameObject root, GameObject model)
    {
        Transform old = root.transform.Find(PropArt.ArtName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var art = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        art.name = PropArt.ArtName;
        art.transform.SetSiblingIndex(0);
        art.transform.localPosition = Vector3.zero;
    }

    static void SetPropArt(GameObject root, string bodyLayer, RuntimeAnimatorController controller)
    {
        if (!root.TryGetComponent(out PropArt propArt)) propArt = root.AddComponent<PropArt>();
        var so = new SerializedObject(propArt);
        so.FindProperty("bodyLayer").stringValue = bodyLayer;
        so.FindProperty("controller").objectReferenceValue = controller;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The model's highest art edge above its pivot, in world units.
    static float Top(GameObject model)
    {
        float top = 0f;
        foreach (SpriteRenderer sr in model.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sprite != null) top = Mathf.Max(top, sr.transform.localPosition.y + sr.sprite.bounds.max.y);
        return top;
    }
}
