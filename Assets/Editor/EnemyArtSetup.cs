using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using Object = UnityEngine.Object;

// Builds an animal enemy from its .aseprite file and the skeleton (Enemy.prefab), from a
// Spec per animal (Wolf, Boar, Deer below):
//   - the importer: 32 PPU like the skeleton and player, the spec's pivot, individual
//     layers incl. hidden ones (a Highlight layer is hidden in Aseprite), tag clips on
//   - <clips>/<tag>.anim: the importer's tag clips, each Aseprite layer's track moved
//     onto the prefab child the skeleton uses for it (Layers below); idle/run/bite
//     loop, death holds; the bite carries EnemyAttacker's events (Spec.swing/hit)
//   - <clips>/<Name> Animator Controller: one state per tag, named like the tag
//     (EnemyAnimator plays them by name; attack/die names are bite/death)
//   - the prefab: a copy of the skeleton with the animal's art and Animator, an art
//     child for each layer the file has (and none for one it lacks), the spec's
//     components removed or added, and its tuning
// Re-runs by itself whenever one of the files is saved (EnemyArtImport), so retimed
// or trimmed tags keep their clips and bite events. Clips, controller and prefab are
// updated in place (same GUIDs); the tuning is only written when the prefab is first
// created, so tweaks on the prefab survive.
static class EnemyArtSetup
{
    class Spec
    {
        public string name;                 // "Wolf": prefab, controller and log name
        public string source;               // the .aseprite file
        public string clipDir;              // Assets/Animation/<name>
        public Vector2 pivot;               // canvas-space, under the body
        public string[] tags;
        public int swingFrame = -1;         // bite events, counted from the bite tag's first frame
        public int hitFrame = -1;           // (-1: no bite)
        public Type[] remove = new Type[0];   // skeleton components this animal doesn't use
        public Action<GameObject> setUp;    // every build: components the behaviour needs
        public Action<GameObject> tune;     // first build only: numbers against the skeleton's

        public string PrefabPath => $"Assets/Prefabs/Entities/Enemy/{name}.prefab";
        public string ControllerPath => $"{clipDir}/{name} Animator Controller.controller";
    }

    const string SkeletonPath = "Assets/Prefabs/Entities/Enemy/Enemy.prefab";
    const float PixelsPerUnit = 32f;

    // The wolf: the skeleton's hunter with a leaping bite. Pivot at the drawn shadow's
    // centre (row 29 of 48); the bite's swing as it leaves the ground (frame 4), the
    // hit mid-leap with the jaws open (frame 5).
    static readonly Spec Wolf = new()
    {
        name = "Wolf",
        source = "Assets/Sprites/Enemy/Wolf/wolf.aseprite",
        clipDir = "Assets/Animation/Wolf",
        pivot = new Vector2(0.5f, 0.4f),
        tags = new[] { "se_idle", "ne_idle", "se_run", "ne_run", "se_bite", "ne_bite", "se_death", "ne_death" },
        swingFrame = 4,
        hitFrame = 5,
        setUp = root => Set(root.GetComponent<EnemyAnimator>(), "attackName", "bite"),
        tune = root =>   // x1.5 speed, x0.7 health, sees further, bites more often
        {
            Set(root.GetComponent<EnemyMover>(), "speed", 0.536f);
            Set(root.GetComponent<CharacterStats>(), "baseHealth", 35f);
            Set(root.GetComponent<EnemyBrain>(), "detectionRadius", 2.5f);
            Set(root.GetComponent<EnemyAttacker>(), "attackCooldown", 2f);
            HurtBox(root, new Vector2(0.4f, 0.2f), 0.15f);
        },
    };

    // The boar: wildlife that never attacks and bolts from whoever hits it
    // (EnemyBrain.Temperament.Skittish + FleeBehavior); it stays in the morning, so no
    // EnemyDespawner. Pivot at the drawn shadow's centre (row 36 of 64), like the wolf.
    static readonly Spec Boar = new()
    {
        name = "Boar",
        source = "Assets/Sprites/Enemy/Boar/boar.aseprite",
        clipDir = "Assets/Animation/Boar",
        pivot = new Vector2(0.5f, 0.4375f),
        tags = new[] { "se_idle", "ne_idle", "se_run", "ne_run", "se_death", "ne_death" },
        remove = Wildlife,
        setUp = Skittish,
        tune = root =>
        {
            Set(root.GetComponent<EnemyMover>(), "speed", 0.357f);
            Set(root.GetComponent<CharacterStats>(), "baseHealth", 30f);
            HurtBox(root, new Vector2(0.4f, 0.25f), 0.11f);
            Set(root.GetComponent<Nameplate>(), "offset", new Vector2(0f, 0.5f));
        },
    };

    // The deer: flees like the boar. Pivot at the drawn shadow's centre (row 46 of 64);
    // its walk clips are copied too (EnemyAnimator plays idle/run/death).
    static readonly Spec Deer = new()
    {
        name = "Deer",
        source = "Assets/Sprites/Enemy/Deer/deer.aseprite",
        clipDir = "Assets/Animation/Deer",
        pivot = new Vector2(0.5f, 0.28125f),
        tags = new[] { "se_idle", "ne_idle", "se_walk", "ne_walk", "se_run", "ne_run", "se_death", "ne_death" },
        remove = Wildlife,
        setUp = Skittish,
        tune = root =>
        {
            Set(root.GetComponent<EnemyMover>(), "speed", 0.357f);
            Set(root.GetComponent<CharacterStats>(), "baseHealth", 25f);
            HurtBox(root, new Vector2(0.4f, 0.35f), 0.28f);   // the torso, not the antlers
            Set(root.GetComponent<Nameplate>(), "offset", new Vector2(0f, 1f));   // over the antlers
        },
    };

    static readonly Spec[] Specs = { Wolf, Boar, Deer };

    // What wildlife doesn't use from the skeleton: no attack, no chase, and it stays in
    // the morning (no EnemyDespawner).
    static Type[] Wildlife => new[] { typeof(EnemyAttacker), typeof(ChaseBehavior), typeof(EnemyDespawner) };

    // Never attacks; bolts from whoever hits it (EnemyBrain.Temperament.Skittish).
    static void Skittish(GameObject root)
    {
        EnemyBrain brain = root.GetComponent<EnemyBrain>();
        if (!root.TryGetComponent(out FleeBehavior flee)) flee = root.AddComponent<FleeBehavior>();
        brain.temperament = EnemyBrain.Temperament.Skittish;
        brain.flee = flee;
        brain.chase = null;
    }

    // prefab child <- the Aseprite layer it shows (first match wins), sorted
    // orderOffset from Body: the drawn shadow under the body with one order free for a
    // blood spray behind it, the target outline behind both (as SkeletonLayerTracks)
    static readonly (string child, string[] layers, int orderOffset)[] Layers =
    {
        ("Body", new[] { "Body", "Layer 1" }, 0),
        ("Ground Shadow", new[] { "Ground Shadow", "Shadow" }, -2),
        (TargetHighlight.LayerName, new[] { TargetHighlight.LayerName }, -3),
    };

    [MenuItem("Tools/Enemies/Build Wolf")]
    static void BuildWolf() => Build(Wolf);

    [MenuItem("Tools/Enemies/Build Boar")]
    static void BuildBoar() => Build(Boar);

    [MenuItem("Tools/Enemies/Build Deer")]
    static void BuildDeer() => Build(Deer);

    // The spec built from `source`, if any (EnemyArtImport).
    internal static bool BuildFor(string source)
    {
        Spec spec = Specs.FirstOrDefault(s => s.source == source);
        if (spec == null) return false;
        Build(spec);
        return true;
    }

    internal static bool IsSource(string path) => Specs.Any(s => s.source == path);

    static void Build(Spec spec)
    {
        if (!ConfigureImporter(spec)) return;

        AnimationClip[] generated = AssetDatabase.LoadAllAssetsAtPath(spec.source).OfType<AnimationClip>().ToArray();
        if (!AssetDatabase.IsValidFolder(spec.clipDir))
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(spec.clipDir), System.IO.Path.GetFileName(spec.clipDir));

        var clips = new AnimationClip[spec.tags.Length];
        for (int i = 0; i < spec.tags.Length; i++)
        {
            AnimationClip from = generated.FirstOrDefault(c => c.name == spec.tags[i]);
            if (from == null)
            {
                Debug.LogError($"[EnemyArtSetup] {spec.source} has no '{spec.tags[i]}' tag clip.");
                return;
            }
            clips[i] = CopyClip(spec, from);
        }

        AnimatorController controller = BuildController(spec, clips);
        ObjectReferenceKeyframe[] idle = AnimationUtility.GetObjectReferenceCurve(clips[0], SpriteTrack("Body"));
        string[] art = generated.SelectMany(AnimationUtility.GetObjectReferenceCurveBindings)
                                .Where(b => b.propertyName == "m_Sprite").Select(b => b.path).Distinct().ToArray();
        BuildPrefab(spec, controller, idle != null && idle.Length > 0 ? idle[0].value as Sprite : null, art);

        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyArtSetup] Built {spec.name}: {clips.Length} clips, the controller and {spec.PrefabPath}.");
    }

    static EditorCurveBinding SpriteTrack(string path) =>
        EditorCurveBinding.PPtrCurve(path, typeof(SpriteRenderer), "m_Sprite");

    // The layer a child shows, among the file's imported layers; null when it has none.
    static string LayerFor(string[] layers, string[] art) => layers.FirstOrDefault(art.Contains);

    // ------------------------------------------------------------------ importer

    static bool ConfigureImporter(Spec spec)
    {
        if (AssetImporter.GetAtPath(spec.source) is not AsepriteImporter importer)
        {
            Debug.LogError($"[EnemyArtSetup] {spec.source} isn't imported by the Aseprite importer.");
            return false;
        }

        bool changed = false;
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)) { importer.spritePixelsPerUnit = PixelsPerUnit; changed = true; }
        if (importer.pivotSpace != PivotSpaces.Canvas) { importer.pivotSpace = PivotSpaces.Canvas; changed = true; }
        if (importer.pivotAlignment != SpriteAlignment.Custom) { importer.pivotAlignment = SpriteAlignment.Custom; changed = true; }
        if (importer.customPivotPosition != spec.pivot) { importer.customPivotPosition = spec.pivot; changed = true; }
        if (importer.layerImportMode != LayerImportModes.IndividualLayers) { importer.layerImportMode = LayerImportModes.IndividualLayers; changed = true; }
        if (!importer.includeHiddenLayers) { importer.includeHiddenLayers = true; changed = true; }
        if (!importer.generateAnimationClips) { importer.generateAnimationClips = true; changed = true; }

        if (changed)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
        return true;
    }

    // ------------------------------------------------------------------ clips

    // The importer's tag clip at <clips>/<tag>.anim (updated in place when it exists),
    // each art layer's tracks (sprite keys, and the importer's on/off keys for frames
    // with no cel) moved onto the prefab child for it.
    static AnimationClip CopyClip(Spec spec, AnimationClip from)
    {
        string path = $"{spec.clipDir}/{from.name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = from.frameRate;

        EditorCurveBinding[] spriteTracks = AnimationUtility.GetObjectReferenceCurveBindings(from);
        string[] art = spriteTracks.Where(b => b.propertyName == "m_Sprite").Select(b => b.path).ToArray();
        foreach ((string child, string[] layers, _) in Layers)
        {
            string layer = LayerFor(layers, art);
            if (layer == null) continue;

            foreach (EditorCurveBinding b in spriteTracks.Where(b => b.path == layer))
            {
                EditorCurveBinding to = b;
                to.path = child;
                AnimationUtility.SetObjectReferenceCurve(clip, to, AnimationUtility.GetObjectReferenceCurve(from, b));
            }
            foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(from).Where(b => b.path == layer))
            {
                EditorCurveBinding to = b;
                to.path = child;
                AnimationUtility.SetEditorCurve(clip, to, AnimationUtility.GetEditorCurve(from, b));
            }
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = !from.name.EndsWith("_death");   // death holds its last frame
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // the bite's attack events, and the run / walk clips' footsteps (FootstepEvents)
        AnimationEvent[] events = from.name.EndsWith("_bite") ? BiteEvents(spec, clip) : new AnimationEvent[0];
        AnimationUtility.SetAnimationEvents(clip, FootstepEvents.With(clip, path, events));

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // EnemyAttacker's swing and hit, on the bite's leap frames. Without OnAttackHit the
    // bite never lands, so a tag too short for them is an error, not a silent skip.
    static AnimationEvent[] BiteEvents(Spec spec, AnimationClip clip)
    {
        ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, SpriteTrack("Body"));
        if (spec.hitFrame < 0 || keys == null || keys.Length <= spec.hitFrame)
        {
            Debug.LogError($"[EnemyArtSetup] '{clip.name}' has {keys?.Length ?? 0} keys; the bite events need frame " +
                           $"{spec.hitFrame}. Update the {spec.name} spec's swingFrame/hitFrame in EnemyArtSetup.cs.", clip);
            return new AnimationEvent[0];
        }
        return new[]
        {
            new AnimationEvent { time = keys[spec.swingFrame].time, functionName = "OnAttackSwing" },
            new AnimationEvent { time = keys[spec.hitFrame].time, functionName = "OnAttackHit" },
        };
    }

    // ------------------------------------------------------------------ controller

    // One state per tag, named like it; EnemyAnimator plays them by name, no transitions.
    static AnimatorController BuildController(Spec spec, AnimationClip[] clips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(spec.ControllerPath)
                                        ?? AnimatorController.CreateAnimatorControllerAtPath(spec.ControllerPath);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        for (int i = 0; i < spec.tags.Length; i++)
        {
            AnimatorState state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == spec.tags[i])
                                  ?? sm.AddState(spec.tags[i], new Vector3(250f + i % 2 * 220f, i / 2 * 60f));
            state.motion = clips[i];
            state.writeDefaultValues = false;
        }
        sm.defaultState = sm.states.First(s => s.state.name == "se_idle").state;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    // ------------------------------------------------------------------ prefab

    static void BuildPrefab(Spec spec, AnimatorController controller, Sprite firstFrame, string[] art)
    {
        bool created = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath) == null;
        if (created && !AssetDatabase.CopyAsset(SkeletonPath, spec.PrefabPath))
        {
            Debug.LogError($"[EnemyArtSetup] Couldn't copy {SkeletonPath} to {spec.PrefabPath}.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(spec.PrefabPath);
        try
        {
            root.name = spec.name;

            // the skeleton's head: an animal is one body
            Transform head = root.transform.Find("Head");
            if (head != null) Object.DestroyImmediate(head.gameObject);

            SpriteRenderer body = root.transform.Find("Body").GetComponent<SpriteRenderer>();
            if (firstFrame != null) body.sprite = firstFrame;

            // an art child per layer the file has; none for one it lacks (the copied
            // skeleton's would keep showing the skeleton)
            foreach ((string child, string[] layers, int orderOffset) in Layers)
            {
                if (orderOffset == 0) continue;
                if (LayerFor(layers, art) != null) EnsureLayer(root, body, child, orderOffset);
                else if (root.transform.Find(child) is Transform stale) Object.DestroyImmediate(stale.gameObject);
            }
            root.GetComponent<Animator>().runtimeAnimatorController = controller;

            foreach (Type type in spec.remove)
            {
                Component c = root.GetComponent(type);
                if (c != null) Object.DestroyImmediate(c);
            }

            // flashes aimed at a removed layer (the skeleton's Head)
            foreach (FlashOnHit flash in root.GetComponents<FlashOnHit>())
                if (new SerializedObject(flash).FindProperty("target").objectReferenceValue == null)
                    Object.DestroyImmediate(flash);

            Set(root.GetComponent<SpriteShadow>(), "layers", new Object[] { body });
            Set(root.GetComponent<EnemyAnimator>(), "dieName", "death");
            spec.setUp?.Invoke(root);
            DropMissing(root.GetComponent<EntityDeath>(), "disableOnDeath");
            AddToDisable(root.GetComponent<EntityDeath>(), root.GetComponent<FleeBehavior>());

            if (created) spec.tune?.Invoke(root);

            // its own voice, feet and flesh -- a fresh copy carries the skeleton's
            AudioPrefabSetup.SetUpCreature(root, spec.name, overwrite: created);

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // A SpriteRenderer beside Body for an art layer, sorted orderOffset from it (kept
    // when it's already there).
    static void EnsureLayer(GameObject root, SpriteRenderer body, string child, int orderOffset)
    {
        if (root.transform.Find(child) != null) return;

        var go = new GameObject(child) { layer = body.gameObject.layer };
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = body.transform.localPosition;
        go.transform.SetSiblingIndex(body.transform.GetSiblingIndex());

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = body.sharedMaterial;
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder + orderOffset;
    }

    static void HurtBox(GameObject root, Vector2 size, float y)
    {
        Transform hurt = root.transform.Find("HurtboxArea");
        if (hurt == null || !hurt.TryGetComponent(out BoxCollider2D box)) return;
        box.size = size;
        box.offset = new Vector2(0f, y);
    }

    // ------------------------------------------------------------------ helpers

    static void Set(Object target, string field, object value)
    {
        if (target == null) { Debug.LogWarning($"[EnemyArtSetup] No component for '{field}'."); return; }
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError($"[EnemyArtSetup] {target.GetType().Name} has no field '{field}'."); return; }

        switch (value)
        {
            case Object[] objects:
                p.arraySize = objects.Length;
                for (int i = 0; i < objects.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                break;
            case float f: p.floatValue = f; break;
            case string s: p.stringValue = s; break;
            case Vector2 v: p.vector2Value = v; break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Removes array entries that pointed at something deleted above.
    static void DropMissing(Object target, string field)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null || !p.isArray) return;
        for (int i = p.arraySize - 1; i >= 0; i--)
            if (p.GetArrayElementAtIndex(i).objectReferenceValue == null) p.DeleteArrayElementAtIndex(i);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The corpse goes limp: switch `behaviour` off on death too (once).
    static void AddToDisable(EntityDeath death, Behaviour behaviour)
    {
        if (death == null || behaviour == null || death.disableOnDeath.Contains(behaviour)) return;
        var so = new SerializedObject(death);
        SerializedProperty p = so.FindProperty("disableOnDeath");
        p.arraySize++;
        p.GetArrayElementAtIndex(p.arraySize - 1).objectReferenceValue = behaviour;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}

// Rebuilds an animal whenever its .aseprite is saved (or its import settings change),
// once the import has finished; the build's own importer fixes reimport the file,
// which runs it once more and changes nothing.
class EnemyArtImport : AssetPostprocessor
{
    static bool scheduled;
    static readonly System.Collections.Generic.HashSet<string> pending = new();

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string path in imported)
            if (EnemyArtSetup.IsSource(path)) pending.Add(path);
        if (scheduled || pending.Count == 0) return;

        scheduled = true;
        EditorApplication.delayCall += () =>
        {
            scheduled = false;
            string[] sources = pending.ToArray();
            pending.Clear();
            foreach (string source in sources) EnemyArtSetup.BuildFor(source);
        };
    }
}
