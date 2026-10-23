using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Builds the screen HUD from the HUD.aseprite mockup. The layout table below is
// the mockup's pixel rects (1920x1080 reference, y down), so the HUD prefab is the
// mockup 1:1; the runtime scripts (Scripts/Render/UI/HUD) only carry behaviour.
//
//   Tools/HUD/Build Pixel Font  -> Fonts/Tiny5/Tiny5 Pixel.asset (bitmap TMP font)
//   Tools/HUD/Build HUD         -> Prefabs/UI/HUD.prefab, the Axe skill tree, the
//                                  player's XP components, and Camp.unity's canvas
//
// Build HUD is safe to re-run and is the source of truth for the layout: it
// overwrites HUD.prefab, so tweak the numbers here rather than the prefab.
static class HudBuilder
{
    const string SpriteDir = HudSpriteImporter.Folder;
    const string TtfPath = "Assets/Fonts/Tiny5/Tiny5-Regular.ttf";
    const string FontPath = "Assets/Fonts/Tiny5/Tiny5 Pixel.asset";
    const string PrefabPath = "Assets/Prefabs/UI/HUD.prefab";
    const string PlayerPrefabPath = "Assets/Prefabs/Entities/Player/Player.prefab";
    const string AxeTreePath = "Assets/Prefabs/Skills/Axe Skill Tree.asset";

    // Tiny5 is drawn on a 128-unit grid at 1024 UPM: 8pt = one font pixel per
    // reference pixel. Text sizes stay whole multiples of 8 so every font pixel is
    // a crisp 2x2 / 3x3 block: small labels at 2x, big labels at 3x.
    const int Small = 16;
    const int Big = 24;
    // With TopLeft alignment the first baseline sits one ascender (7 font px) under
    // the rect's top, and capitals are 5 font px tall.
    static int CapTop(int size) => size * 2 / 8;   // capital's top row below the rect's top
    static int Cap(int size) => size * 5 / 8;      // capital height

    // The hand bar group's centre in the mockup (736..1136); it's anchored so this
    // lands on the screen's centre at any resolution.
    const int HandBarCentre = 936;

    // The mockup has no warmth bar; it sits under health and pushes the menu down.
    const int WarmthShift = 18;

    static readonly Color White = new(241f / 255f, 246f / 255f, 240f / 255f);
    static readonly Color Black = Color.black;

    static TMP_FontAsset font;

    // ------------------------------------------------------------------ font

    [MenuItem("Tools/HUD/Build Pixel Font")]
    static void BuildFontMenu() => BuildFont();

    static TMP_FontAsset BuildFont()
    {
        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (ttf == null) { Debug.LogError($"[HudBuilder] Missing {TtfPath}"); return null; }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null) AssetDatabase.DeleteAsset(FontPath);

        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(ttf, Small, 1, GlyphRenderMode.RASTER_HINTED, 256, 256);
        fa.name = "Tiny5 Pixel";
        AssetDatabase.CreateAsset(fa, FontPath);

        // the atlas and material are separate objects: without being sub-assets they
        // are lost on the next domain reload and the font renders nothing
        fa.atlasTexture.name = "Tiny5 Pixel Atlas";
        fa.material.name = "Tiny5 Pixel Material";
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        AssetDatabase.AddObjectToAsset(fa.material, fa);

        string ascii = "";
        for (char c = ' '; c <= '~'; c++) ascii += c;
        fa.TryAddCharacters(ascii, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[HudBuilder] Tiny5 lacks: {missing}");

        fa.atlasTexture.filterMode = FilterMode.Point;
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HudBuilder] Built {FontPath}");
        return fa;
    }

    // ------------------------------------------------------------------- HUD

    [MenuItem("Tools/HUD/Build HUD")]
    static void BuildHud()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) font = BuildFont();
        if (font == null) return;

        SkillTree axeTree = EnsureAxeTree();
        EnsurePlayerComponents(axeTree);

        GameObject root = BuildHierarchy();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        InstallInScene(prefab);
        Debug.Log("[HudBuilder] HUD built and installed in the open scene.");
    }

    static SkillTree EnsureAxeTree()
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTree>(AxeTreePath);
        if (tree != null)
        {
            // migrate: the big 0/5 node became the Whirlwind spell
            for (int i = 0; i < tree.nodes.Length; i++)
                if (tree.nodes[i].id == "axe_c") tree.nodes[i] = WhirlwindNode();
            SkillNode spell = tree.Find("axe_whirlwind");
            if (spell != null && spell.icon == null) spell.icon = S("icon_spell_whirlwind");
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssetIfDirty(tree);
            return tree;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Skills")) AssetDatabase.CreateFolder("Assets/Prefabs", "Skills");
        tree = ScriptableObject.CreateInstance<SkillTree>();
        tree.weapon = ToolKind.Axe;
        tree.displayName = "Axe";
        tree.icon = S("icon_weapon_axe");
        // positions from the mockup: small nodes at 1271/1343,197, big node at 1309,284
        tree.nodes = new[]
        {
            new SkillNode { id = "axe_a", displayName = "Axe Skill A", maxRank = 2, size = SkillNodeSize.Small, position = new Vector2(0, 0) },
            new SkillNode { id = "axe_b", displayName = "Axe Skill B", maxRank = 2, size = SkillNodeSize.Small, position = new Vector2(72, 0) },
            WhirlwindNode(),
        };
        AssetDatabase.CreateAsset(tree, AxeTreePath);
        return tree;
    }

    // The Axe tree's spell (big node): learned once, needs both small nodes maxed.
    static SkillNode WhirlwindNode() => new()
    {
        id = "axe_whirlwind",
        displayName = "Whirlwind",
        description = "Spin with your axe held out, striking everything around you on every turn.",
        kind = SkillNodeKind.Spell,
        maxRank = 1,
        size = SkillNodeSize.Big,
        position = new Vector2(38, 87),
        parents = new[] { "axe_a", "axe_b" },
        icon = S("icon_spell_whirlwind"),
    };

    static void EnsurePlayerComponents(SkillTree axeTree)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (!player.TryGetComponent(out PlayerExperience _)) player.AddComponent<PlayerExperience>();
            if (!player.TryGetComponent(out ActiveSkillSlots _)) player.AddComponent<ActiveSkillSlots>();
            if (!player.TryGetComponent(out WeaponExperience wx)) wx = player.AddComponent<WeaponExperience>();
            Wire(wx, ("trees", new Object[] { axeTree }));
            if (!player.TryGetComponent(out WhirlwindSkill whirlwind)) whirlwind = player.AddComponent<WhirlwindSkill>();
            Wire(whirlwind, ("icon", S("icon_spell_whirlwind")));
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    static GameObject BuildHierarchy()
    {
        var root = new GameObject("HUD", typeof(RectTransform), typeof(CanvasGroup));
        Stretch((RectTransform)root.transform);
        HudRoot hud = root.AddComponent<HudRoot>();

        BuildStats(root.transform, hud);
        BuildMenu(root.transform, hud);
        BuildHandBar(root.transform, hud);
        return root;
    }

    // --- top-left stats (mockup 87,21 .. 389,47) ---

    static void BuildStats(Transform parent, HudRoot hud)
    {
        RectTransform stats = Child("Stats", parent);
        Stretch(stats);

        Image xpFill = Filled("XP Fill", stats, S("bar_xp_fill"), 88, 22);
        Img("XP Frame", stats, S("bar_xp_frame"), 87, 21);
        TMP_Text level = Text("Level", stats, "1", 205, 15 - CapTop(Big), 40, Big, White, outline: true);

        Image healthFill = Filled("Health Fill", stats, S("bar_health_fill"), 89, 33);
        Img("Health Frame", stats, S("bar_health_frame"), 87, 31);
        TMP_Text health = Text("Health Text", stats, "100/100", 331, 32 - CapTop(Big), 100, Big, White, outline: true);

        Image warmthFill = Filled("Warmth Fill", stats, S("bar_warmth_fill"), 89, 33 + WarmthShift);
        Img("Warmth Frame", stats, S("bar_health_frame"), 87, 31 + WarmthShift);

        PlayerStatsUI ui = stats.gameObject.AddComponent<PlayerStatsUI>();
        Wire(ui, ("root", hud), ("xpFill", xpFill), ("levelText", level), ("healthFill", healthFill),
             ("healthText", health), ("warmthFill", warmthFill));
    }

    // --- F menu: tabs at mockup y=49, pages at 87,84 (+ the warmth bar's shift) ---

    static void BuildMenu(Transform parent, HudRoot hud)
    {
        RectTransform menu = Child("Menu", parent);
        Stretch(menu);
        RectTransform panel = Child("Panel", menu);
        Stretch(panel);

        int y = 49 + WarmthShift;
        TabButtonUI playerTab = Tab("Player Tab", panel, "tab", 87, y, S("icon_tab_player"), 25, 7);
        TabButtonUI craftTab = Tab("Crafting Tab", panel, "tab", 154, y, S("icon_tab_crafting"), 30, 7);
        TabButtonUI skillsTab = Tab("Skills Tab", panel, "tab", 220, y, S("icon_tab_skills"), 20, 4);

        RectTransform playerPage = Page("Player Page", panel);
        RectTransform craftPage = Page("Crafting Page", panel);
        RectTransform skillsPage = Page("Skills Page", panel);
        BuildPlayerPage(playerPage, hud);
        BuildCraftingPage(craftPage, hud);
        BuildSkillsPage(skillsPage, hud);

        HudMenu ui = menu.gameObject.AddComponent<HudMenu>();
        Wire(ui, ("panel", panel.gameObject),
             ("tabs", new Object[] { playerTab, craftTab, skillsTab }),
             ("pages", new Object[] { playerPage.gameObject, craftPage.gameObject, skillsPage.gameObject }));
    }

    static RectTransform Page(string name, Transform parent) => Box(name, parent, 87, 84 + WarmthShift, 0, 0);

    // mockup Player page at 87,84: head/chest/legs column, ring/amulet beside it
    static void BuildPlayerPage(RectTransform page, HudRoot hud)
    {
        EquipSlotUI head = EquipBox("Head", page, EquipSlot.Head, "slot_64", 0, 0, "equip_ph_head", 13, 12);
        EquipSlotUI chest = EquipBox("Chest", page, EquipSlot.Chest, "slot_64", 0, 67, "equip_ph_chest", 10, 12);
        EquipSlotUI legs = EquipBox("Legs", page, EquipSlot.Legs, "slot_64", 0, 133, "equip_ph_legs", 22, 14);
        EquipSlotUI ring = EquipBox("Ring", page, EquipSlot.Ring, "slot_64x46", 66, 53, "equip_ph_ring", 16, 5);
        EquipSlotUI amulet = EquipBox("Amulet", page, EquipSlot.Amulet, "slot_64x46", 66, 101, "equip_ph_amulet", 20, 6);

        EquipmentTabUI ui = page.gameObject.AddComponent<EquipmentTabUI>();
        Wire(ui, ("root", hud), ("slots", new Object[] { head, chest, legs, ring, amulet }));
    }

    static EquipSlotUI EquipBox(string name, Transform parent, EquipSlot slot, string frame, int x, int y,
                                string placeholder, int phX, int phY)
    {
        Image box = Img(name, parent, S(frame), x, y);
        box.raycastTarget = true;
        Button button = box.gameObject.AddComponent<Button>();
        NoNavigation(button);

        Image ph = Img("Placeholder", box.transform, S(placeholder), phX, phY);
        Image icon = CenteredIcon(box.transform);

        EquipSlotUI ui = box.gameObject.AddComponent<EquipSlotUI>();
        Wire(ui, ("slot", slot), ("placeholder", ph), ("icon", icon));
        return ui;
    }

    // mockup Crafting page at 588,85 (shifted onto the Player page's origin)
    static void BuildCraftingPage(RectTransform page, HudRoot hud)
    {
        TabButtonUI hands = TextTab("Hands Tab", page, 0, 0, "Hands", out int handsWidth);
        TabButtonUI bench = TextTab("Workbench Tab", page, handsWidth + 3, 0, "Workbench", out _);

        RectTransform grid = Box("Grid", page, 0, 29, 4 * 64 + 3 * 3, 3 * 64 + 2 * 2);
        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(64, 64);
        layout.spacing = new Vector2(3, 2);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;

        Image cellImage = Img("Blueprint", grid, S("slot_64"), 0, 0);
        cellImage.raycastTarget = true;
        NoNavigation(cellImage.gameObject.AddComponent<Button>());
        cellImage.gameObject.AddComponent<CanvasGroup>();
        BlueprintCellUI cell = cellImage.gameObject.AddComponent<BlueprintCellUI>();
        Wire(cell, ("icon", CenteredIcon(cellImage.transform)));
        cellImage.gameObject.SetActive(false);

        // hovered blueprint: name at mockup 933,113, ingredients listed under it
        // (each row: a 32px icon box -- a 16px item icon at 2x -- with the label beside it)
        const int rowHeight = 34;
        RectTransform details = Box("Details", page, 345, 28 - CapTop(Big), 0, 0);
        TMP_Text name = Text("Name", details, "Campfire", 0, 0, 300, Big, White, outline: true);
        RectTransform list = Box("Ingredients", details, 0, CapTop(Big) + Cap(Big) + 8, 0, 0);
        RectTransform row = Box("Ingredient", list, 0, 0, 300, 32);
        Image rowIcon = Img("Icon", row, null, 0, 0);
        rowIcon.rectTransform.sizeDelta = new Vector2(32, 32);
        TMP_Text rowLabel = Text("Label", row, "2 Stick", 38, (32 - Cap(Small)) / 2 - CapTop(Small), 260, Small, White, outline: true);
        IngredientRowUI rowUi = row.gameObject.AddComponent<IngredientRowUI>();
        Wire(rowUi, ("icon", rowIcon), ("label", rowLabel), ("iconBox", new Vector2(32, 32)));
        row.gameObject.SetActive(false);
        details.gameObject.SetActive(false);

        CraftingTabUI ui = page.gameObject.AddComponent<CraftingTabUI>();
        Wire(ui, ("root", hud), ("handsTab", hands), ("workbenchTab", bench), ("grid", grid),
             ("cellTemplate", cell), ("details", details.gameObject), ("nameText", name),
             ("ingredientList", list), ("rowTemplate", rowUi), ("rowHeight", (float)rowHeight));
    }

    // mockup Skills page at 1194,84 (shifted onto the Player page's origin)
    static void BuildSkillsPage(RectTransform page, HudRoot hud)
    {
        string[] names = { "Axe", "Sword", "Pickaxe", "Wand", "Bow" };
        ToolKind[] kinds = { ToolKind.Axe, ToolKind.Sword, ToolKind.Pickaxe, ToolKind.Wand, ToolKind.Bow };
        var tabs = new Object[names.Length];
        int x = 0;
        for (int i = 0; i < names.Length; i++)
        {
            tabs[i] = TextTab(names[i] + " Tab", page, x, 0, names[i], out int width);
            x += width + 3;
        }

        Image icon = Img("Weapon Icon", page, S("icon_weapon_axe"), 142, 41);

        Image xpFill = Filled("Weapon XP Fill", page, S("bar_xp_fill"), 89, 80);
        Img("Weapon XP Frame", page, S("bar_xp_frame"), 88, 79);
        TMP_Text level = Text("Weapon Level", page, "1", 209, 76 - CapTop(Big), 40, Big, White, outline: true);
        TMP_Text points = Text("Points", page, "Points: 0", 100, 94 - CapTop(Big), 140, Big, White, outline: true);

        RectTransform treeRect = Box("Tree", page, 77, 113, 0, 0);
        SkillNodeUI small = NodeTemplate("Small Node", treeRect, S("node_small"));
        SkillNodeUI big = NodeTemplate("Big Node", treeRect, S("slot_64"));
        Image line = Img("Line", treeRect, S("ui_pixel"), 0, 0);
        line.gameObject.SetActive(false);
        SkillTreeUI tree = treeRect.gameObject.AddComponent<SkillTreeUI>();
        Wire(tree, ("smallTemplate", small), ("bigTemplate", big), ("lineTemplate", line));

        SkillsTabUI ui = page.gameObject.AddComponent<SkillsTabUI>();
        Wire(ui, ("root", hud), ("weaponTabs", tabs), ("weaponKinds", kinds), ("weaponIcon", icon),
             ("xpFill", xpFill), ("levelText", level), ("pointsText", points), ("tree", tree));
    }

    static SkillNodeUI NodeTemplate(string name, Transform parent, Sprite frame)
    {
        Image box = Img(name, parent, frame, 0, 0);
        box.raycastTarget = true;
        NoNavigation(box.gameObject.AddComponent<Button>());
        box.gameObject.AddComponent<CanvasGroup>();
        Vector2 size = box.rectTransform.sizeDelta;
        Image icon = CenteredIcon(box.transform);   // a spell's icon (big node); hidden when none
        // "0/2" in the bottom-right corner (mockup: 3px in from the right, 4 up)
        TMP_Text rank = Text("Rank", box.transform, "0/2", 0, (int)size.y - 4 - Cap(Small) - CapTop(Small), (int)size.x - 2,
                             Small, White, align: TextAlignmentOptions.TopRight);
        SkillNodeUI ui = box.gameObject.AddComponent<SkillNodeUI>();
        Wire(ui, ("rankText", rank), ("icon", icon), ("iconBox", size - new Vector2(16, 16)));
        box.gameObject.SetActive(false);
        return ui;
    }

    // --- hand bar (mockup 706..1136 x 943..1077), anchored bottom-centre ---

    static void BuildHandBar(Transform parent, HudRoot hud)
    {
        RectTransform bar = Child("Hand Bar", parent);
        Stretch(bar);

        HandSlotUI left = HandSlot("Left Hand", bar, 736, 1052, "hint_mouse_lmb", -12, 3);
        ActionSlotUI action1 = ActionSlot("Action 1", bar, 848, "key_1");
        ActionSlotUI action2 = ActionSlot("Action 2", bar, 944, "key_2");
        HandSlotUI right = HandSlot("Right Hand", bar, 1040, 1052, "hint_mouse_rmb", -5, 4);

        Image scroll = Bottom("Scroll Hint", bar, S("hint_scroll"), 715, 1037);

        RectTransform drop = Bottom("Drop Hint", bar, null, 769, 1077).rectTransform;
        Object.DestroyImmediate(drop.GetComponent<Image>());
        drop.sizeDelta = new Vector2(64, 22);
        Img("Q", drop, S("key_q"), 0, 0);
        Text("Drop", drop, "Drop", 27, (22 - Cap(Small)) / 2 - CapTop(Small), 40, Small, Black);

        Bottom("Tab Hint", bar, S("hint_tab"), 906, 1077);

        HandBarUI ui = bar.gameObject.AddComponent<HandBarUI>();
        Wire(ui, ("root", hud), ("leftHand", left), ("rightHand", right), ("leftAction", action1),
             ("rightAction", action2), ("scrollHint", scroll.rectTransform), ("dropHint", drop));
    }

    static HandSlotUI HandSlot(string name, Transform parent, int x, int bottom, string mouse, int hintX, int hintGap)
    {
        Image frame = Bottom(name, parent, S("hand_frame"), x, bottom);
        Image icon = CenteredIcon(frame.transform);
        TMP_Text count = Text("Count", frame.transform, "", 0, 0, 0, Big, White, outline: true,
                              align: TextAlignmentOptions.BottomRight);
        RectTransform countRt = count.rectTransform;
        countRt.anchorMin = countRt.anchorMax = countRt.pivot = new Vector2(1, 0);
        countRt.sizeDelta = new Vector2(60, 30);
        countRt.anchoredPosition = new Vector2(-4, 4);

        // mouse hint rides on the frame's top edge
        Image hint = Img("Mouse Hint", frame.transform, S(mouse), 0, 0);
        RectTransform hrt = hint.rectTransform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 1f);
        hrt.pivot = new Vector2(0, 0);
        hrt.anchoredPosition = new Vector2(hintX, hintGap);

        HandSlotUI ui = frame.gameObject.AddComponent<HandSlotUI>();
        Wire(ui, ("frame", frame), ("normalSprite", S("hand_frame")), ("selectedSprite", S("hand_frame_selected")),
             ("icon", icon), ("countText", count));
        return ui;
    }

    static ActionSlotUI ActionSlot(string name, Transform parent, int x, string key)
    {
        Image frame = Bottom(name, parent, S("action_slot"), x, 1052);
        Image icon = CenteredIcon(frame.transform);

        Image cooldown = Img("Cooldown", frame.transform, S("ui_pixel"), 2, 2);
        cooldown.rectTransform.sizeDelta = new Vector2(76, 60);
        cooldown.color = new Color(0, 0, 0, 0.5f);
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Vertical;
        cooldown.fillOrigin = (int)Image.OriginVertical.Top;
        cooldown.fillAmount = 0;
        cooldown.enabled = false;

        Img("Key Hint", frame.transform, S(key), 29, -26);   // mockup: 4px above the slot

        ActionSlotUI ui = frame.gameObject.AddComponent<ActionSlotUI>();
        Wire(ui, ("icon", icon), ("cooldown", cooldown));
        return ui;
    }

    // ------------------------------------------------------------- scene

    static void InstallInScene(GameObject prefab)
    {
        var gsm = Object.FindFirstObjectByType<GameStateManager>(FindObjectsInactive.Include);
        if (gsm == null || gsm.mainMenuPanel == null)
        {
            Debug.LogError("[HudBuilder] Open Camp.unity first (needs its GameStateManager to find the canvas).");
            return;
        }
        Transform canvas = gsm.mainMenuPanel.transform.parent;

        // old UI: the journal, the 2-slot inventory bar, the health/warmth player bar
        foreach (string old in new[] { "JournalPanel", "InventoryBar", "PlayerBar", "HUD" })
        {
            Transform t = canvas.Find(old);
            while (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
                t = canvas.Find(old);
            }
        }
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(canvas.gameObject);   // HealthRender, JournalUI

        var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        hud.transform.SetSiblingIndex(0);   // under the pause / game over / menu panels

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
    }

    // ------------------------------------------------------------- helpers

    static Sprite S(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
        if (sprite == null) Debug.LogError($"[HudBuilder] Missing sprite {name} (run Tools/gen_hud_sprites.py)");
        return sprite;
    }

    static RectTransform Child(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Rect at mockup-style coordinates: top-left of the parent, y down.
    static RectTransform Box(string name, Transform parent, int x, int y, int w, int h)
    {
        RectTransform rt = Child(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Image Img(string name, Transform parent, Sprite sprite, int x, int y)
    {
        Vector2 size = sprite != null ? sprite.rect.size : Vector2.zero;
        RectTransform rt = Box(name, parent, x, y, (int)size.x, (int)size.y);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    static Image Filled(string name, Transform parent, Sprite sprite, int x, int y)
    {
        Image img = Img(name, parent, sprite, x, y);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        return img;
    }

    // Hand-bar element given by its mockup left x and bottom y, anchored to the
    // screen's bottom centre so the group stays centred at every resolution.
    static Image Bottom(string name, Transform parent, Sprite sprite, int x, int bottom)
    {
        Image img = Img(name, parent, sprite, 0, 0);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = Vector2.zero;   // frames grow upward when the selected hand gets taller
        rt.anchoredPosition = new Vector2(x - HandBarCentre, 1080 - bottom);
        return img;
    }

    static Image CenteredIcon(Transform parent)
    {
        RectTransform rt = Child("Icon", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }

    static TMP_Text Text(string name, Transform parent, string text, int x, int y, int w, int size, Color color,
                         bool outline = false, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        RectTransform rt = Box(name, parent, x, y, w, size * 2);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.text = text;
        if (outline) rt.gameObject.AddComponent<PixelOutlineText>();
        return t;
    }

    static TabButtonUI Tab(string name, Transform parent, string kind, int x, int y, Sprite icon, int iconX, int iconY)
    {
        TabButtonUI tab = TabBase(name, parent, kind, x, y);
        Img("Icon", tab.transform, icon, iconX, iconY);
        return tab;
    }

    // Sub-tab with a small white label centred on it (whole pixels, so the bitmap
    // font stays crisp). The frame is 9-sliced: it keeps the mockup's 56px width
    // unless the label needs more, then grows to the label plus padding.
    const int SubTabMinWidth = 56;
    const int SubTabPadding = 8;

    static TabButtonUI TextTab(string name, Transform parent, int x, int y, string label, out int width)
    {
        TabButtonUI tab = TabBase(name, parent, "subtab", x, y);
        var rt = (RectTransform)tab.transform;
        TMP_Text t = Text("Label", tab.transform, label, 0, 0, 200, Small, White);
        int textWidth = Mathf.CeilToInt(t.GetPreferredValues(label).x) - Small / 8;   // minus the last advance's trailing gap

        width = Mathf.Max(SubTabMinWidth, textWidth + 2 * SubTabPadding);
        tab.GetComponent<Image>().type = Image.Type.Sliced;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);

        int top = ((int)rt.sizeDelta.y - Cap(Small)) / 2;
        ((RectTransform)t.transform).anchoredPosition = new Vector2((width - textWidth) / 2, -(top - CapTop(Small)));
        return tab;
    }

    static TabButtonUI TabBase(string name, Transform parent, string kind, int x, int y)
    {
        Image img = Img(name, parent, S(kind + "_unselected"), x, y);
        img.raycastTarget = true;
        NoNavigation(img.gameObject.AddComponent<Button>());
        img.gameObject.AddComponent<CanvasGroup>();
        TabButtonUI tab = img.gameObject.AddComponent<TabButtonUI>();
        Wire(tab, ("selectedSprite", S(kind + "_selected")), ("unselectedSprite", S(kind + "_unselected")));
        return tab;
    }

    // No keyboard/gamepad navigation: WASD would otherwise move focus between
    // buttons and Space/Enter re-press the last one (e.g. crafting again).
    static void NoNavigation(Button button)
    {
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;   // disabled tabs dim through their CanvasGroup instead
        button.colors = colors;
    }

    // Assign serialized fields by name (they're private [SerializeField]s).
    static void Wire(Object target, params (string field, object value)[] fields)
    {
        var so = new SerializedObject(target);
        foreach ((string field, object value) in fields)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[HudBuilder] {target.GetType().Name} has no field '{field}'"); continue; }

            switch (value)
            {
                case Object[] objects:
                    p.arraySize = objects.Length;
                    for (int i = 0; i < objects.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
                    break;
                case ToolKind[] kinds:
                    p.arraySize = kinds.Length;
                    for (int i = 0; i < kinds.Length; i++) p.GetArrayElementAtIndex(i).intValue = (int)kinds[i];
                    break;
                case Object o: p.objectReferenceValue = o; break;
                case Enum e: p.intValue = Convert.ToInt32(e); break;
                case float f: p.floatValue = f; break;
                case Vector2 v: p.vector2Value = v; break;
                default: Debug.LogError($"[HudBuilder] Unsupported value for {field}"); break;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
