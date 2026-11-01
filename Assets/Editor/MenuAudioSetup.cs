using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools/Audio/Set Up Menu Audio, on the open scene (Camp): the menus' sounds and the
// player's volume settings (SoundSystemPlan Phase 5).
//   - ButtonSounds (hover tick, click) on every Button of GameStateManager's four panels:
//     main menu, pause, game over, won
//   - an "Audio Settings" section on the pause panel, above its buttons: a row per channel
//     (Master, Music, Ambience, SFX, UI) with a label and a slider, and the "mute when
//     unfocused" toggle, bound by AudioSettingsPanel. Its text uses the panel's own
//     button font, in white over a dark backing. Built fresh each run (the old section is replaced).
// Save the scene afterwards.
public static class MenuAudioSetup
{
    const string SectionName = "Audio Settings";
    static readonly string[] Labels = { "Master", "Music", "Ambience", "Effects", "Interface" };

    [MenuItem("Tools/Audio/Set Up Menu Audio")]
    static void SetUp()
    {
        GameStateManager state = Object.FindObjectsByType<GameStateManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (state == null)
        {
            EditorUtility.DisplayDialog("Set Up Menu Audio", "No GameStateManager in the open scene. Open Camp first.", "OK");
            return;
        }

        int buttons = 0;
        foreach (GameObject panel in new[] { state.mainMenuPanel, state.pausePanel, state.gameOverPanel, state.gameWonPanel })
        {
            if (panel == null) continue;
            foreach (Button b in panel.GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<ButtonSounds>() != null || b.GetComponentInParent<AudioSettingsPanel>(true) != null) continue;
                Undo.AddComponent<ButtonSounds>(b.gameObject);
                buttons++;
            }
        }

        if (state.pausePanel != null) BuildSettings(state.pausePanel);

        EditorSceneManager.MarkSceneDirty(state.gameObject.scene);
        Debug.Log($"[MenuAudioSetup] Button sounds on {buttons} new buttons; the Audio section is on the pause panel. Save the scene.");
    }

    static void BuildSettings(GameObject pausePanel)
    {
        Transform old = pausePanel.transform.Find(SectionName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        TextMeshProUGUI like = pausePanel.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();

        var section = new GameObject(SectionName, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(section, "Audio Settings");
        RectTransform rt = Rect(section, pausePanel.transform, new Vector2(0f, 60f), new Vector2(440f, 300f));
        section.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        Text(section.transform, "Title", "Audio", like, 26, TextAlignmentOptions.Center, new Vector2(0f, 120f), new Vector2(400f, 36f));

        var resources = new DefaultControls.Resources();
        var sliders = new Slider[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
        {
            float y = 76f - i * 38f;
            Text(section.transform, Labels[i] + " Label", Labels[i], like, 20, TextAlignmentOptions.Right, new Vector2(-105f, y), new Vector2(150f, 30f));

            GameObject slider = DefaultControls.CreateSlider(resources);
            slider.name = Labels[i] + " Slider";
            Rect(slider, section.transform, new Vector2(85f, y), new Vector2(200f, 20f));
            Slider s = slider.GetComponent<Slider>();
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = 1f;
            sliders[i] = s;
        }

        GameObject toggle = DefaultControls.CreateToggle(resources);
        toggle.name = "Mute Unfocused Toggle";
        Rect(toggle, section.transform, new Vector2(-60f, -118f), new Vector2(24f, 24f));
        // the default label is legacy Text; use the menu's own font instead
        Transform legacy = toggle.transform.Find("Label");
        if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
        Text(section.transform, "Mute Label", "Mute when unfocused", like, 18, TextAlignmentOptions.Left, new Vector2(70f, -118f), new Vector2(240f, 28f));

        AudioSettingsPanel panel = section.AddComponent<AudioSettingsPanel>();
        var so = new SerializedObject(panel);
        SerializedProperty list = so.FindProperty("sliders");
        list.arraySize = sliders.Length;
        for (int i = 0; i < sliders.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sliders[i];
        so.FindProperty("muteUnfocused").objectReferenceValue = toggle.GetComponent<Toggle>();
        so.ApplyModifiedPropertiesWithoutUndo();

        rt.SetAsFirstSibling();   // under the panel's buttons
    }

    static RectTransform Rect(GameObject go, Transform parent, Vector2 pos, Vector2 size)
    {
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Text(Transform parent, string name, string text, TextMeshProUGUI like, float size,
                     TextAlignmentOptions align, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Rect(go, parent, pos, box);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (like != null) t.font = like.font;
        t.color = Color.white;   // over the section's dark backing
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.raycastTarget = false;
    }
}
