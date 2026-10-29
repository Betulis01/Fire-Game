using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Hover tooltip for an item (bag, loadout and equipment boxes -- see ItemHoverTooltip):
// its name, the stats it gives while worn and, for a weapon, its damage. Drawn in
// the 9-sliced tooltip_frame sized to its lines, following the pointer and kept on
// screen. Lines are pooled clones of a template placed on whole pixels, so the
// bitmap font stays crisp. Never blocks raycasts (it would steal the hover).
public class ItemTooltip : MonoBehaviour
{
    [SerializeField] RectTransform panel;      // the frame; top-left anchor and pivot
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text lineTemplate;    // inactive; cloned per stat line

    [Tooltip("Frame border + gap around the text, in reference pixels.")]
    [SerializeField] int padding = 8;
    [Tooltip("Distance between line tops.")]
    [SerializeField] int rowStep = 20;
    [Tooltip("Capital height and the gap above it inside a line's rect (Small font).")]
    [SerializeField] int capHeight = 10;
    [SerializeField] int capTop = 4;
    [Tooltip("Panel's top-left offset from the pointer (right / down).")]
    [SerializeField] Vector2Int cursorOffset = new(16, 16);

    readonly List<TMP_Text> rows = new();
    readonly List<string> lines = new();
    GameObject shown;
    RectTransform area;   // this object: stretched over the HUD

    void Awake()
    {
        area = (RectTransform)transform;
        Hide(null);
    }

    public void Show(GameObject item)
    {
        if (item == null) { Hide(null); return; }
        shown = item;
        nameText.text = Describe(item, lines);

        while (rows.Count < lines.Count)
            rows.Add(Instantiate(lineTemplate, lineTemplate.transform.parent));

        int width = Width(nameText);
        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < lines.Count;
            rows[i].gameObject.SetActive(used);
            if (!used) continue;
            rows[i].text = lines[i];
            rows[i].rectTransform.anchoredPosition = new Vector2(padding, -(padding + (i + 1) * rowStep - capTop));
            width = Mathf.Max(width, Width(rows[i]));
        }

        nameText.rectTransform.anchoredPosition = new Vector2(padding, -(padding - capTop));
        panel.sizeDelta = new Vector2(width + 2 * padding, 2 * padding + capHeight + lines.Count * rowStep);
        panel.gameObject.SetActive(true);
        Follow();
    }

    // Hide, if `item` is what's showing (null hides whatever is).
    public void Hide(GameObject item)
    {
        if (item != null && item != shown) return;
        shown = null;
        if (panel != null) panel.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (shown == null) return;
        Follow();
    }

    // Top-left at the pointer + offset, flipped to the other side of the pointer
    // where it would run off the screen.
    void Follow()
    {
        if (UserInput.Instance == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, UserInput.Instance.PointerScreen, null, out Vector2 local))
            return;

        Rect r = area.rect;
        Vector2 size = panel.sizeDelta;
        float x = local.x - r.xMin + cursorOffset.x;
        float y = r.yMax - local.y + cursorOffset.y;   // from the top, downward
        if (x + size.x > r.width) x -= size.x + 2 * cursorOffset.x;
        if (y + size.y > r.height) y -= size.y + 2 * cursorOffset.y;
        panel.anchoredPosition = new Vector2(Mathf.Round(Mathf.Max(0f, x)), -Mathf.Round(Mathf.Max(0f, y)));
    }

    static int Width(TMP_Text t) => Mathf.CeilToInt(t.GetPreferredValues(t.text).x);

    // The item's name, with its stat and damage lines filled into `lines`.
    public static string Describe(GameObject item, List<string> lines)
    {
        lines.Clear();

        if (item.TryGetComponent(out Tool tool) &&
            (item.TryGetComponent(out MeleeWeapon _) || item.TryGetComponent(out RangedWeapon _)))
        {
            lines.Add($"Damage {tool.minDamage:0}-{tool.maxDamage:0}");
            if (tool.heavy != null && tool.heavy.damageCoefficient > 0f)
                lines.Add($"Heavy {tool.heavy.damageCoefficient * 100f:0}%");
        }

        if (item.TryGetComponent(out Equippable gear))
        {
            StatBlock s = gear.stats;
            AddStat(lines, s.strength, "Strength");
            AddStat(lines, s.agility, "Agility");
            AddStat(lines, s.intelligence, "Intelligence");
            AddStat(lines, s.stamina, "Stamina");
            AddStat(lines, s.armor, "Armor");
            AddStat(lines, s.maxHealth, "Max Health");
        }

        return item.TryGetComponent(out WorldItem wi) && wi.item != null && !string.IsNullOrEmpty(wi.item.displayName)
            ? wi.item.displayName
            : item.name.Replace("(Clone)", "").Trim();
    }

    static void AddStat(List<string> lines, float value, string label)
    {
        if (Mathf.Approximately(value, 0f)) return;
        lines.Add($"{(value > 0f ? "+" : "")}{value:0.#} {label}");
    }
}
