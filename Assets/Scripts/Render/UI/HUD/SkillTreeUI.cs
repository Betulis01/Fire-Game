using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Lays out one weapon's SkillTree: a node box per SkillNode at its authored
// position (small or big template), joined to its parents by 1px elbow lines
// (down from the parent, across, down into the child). Rebuilt when the shown
// weapon changes; ranks refresh in place.
public class SkillTreeUI : MonoBehaviour
{
    [SerializeField] SkillNodeUI smallTemplate;   // inactive children
    [SerializeField] SkillNodeUI bigTemplate;
    [SerializeField] Image lineTemplate;          // 1px white sprite, tinted
    [SerializeField] Color lineColor = new(64f / 255f, 82f / 255f, 115f / 255f);
    [SerializeField] float lineWidth = 1f;

    readonly List<SkillNodeUI> nodes = new();
    readonly List<GameObject> lines = new();
    WeaponExperience progress;
    ToolKind weapon;

    void Awake()
    {
        smallTemplate.gameObject.SetActive(false);
        bigTemplate.gameObject.SetActive(false);
        lineTemplate.gameObject.SetActive(false);
    }

    public void Show(SkillTree tree, WeaponExperience weaponProgress)
    {
        Clear();
        progress = weaponProgress;
        if (tree == null) return;
        weapon = tree.weapon;

        var byId = new Dictionary<string, SkillNodeUI>();
        foreach (SkillNode node in tree.nodes)
        {
            SkillNodeUI template = node.size == SkillNodeSize.Big ? bigTemplate : smallTemplate;
            SkillNodeUI ui = Instantiate(template, transform);
            ui.gameObject.SetActive(true);
            ui.Bind(node);
            ui.Rect.anchoredPosition = new Vector2(node.position.x, -node.position.y);
            ui.Clicked += OnClickNode;
            nodes.Add(ui);
            byId[node.id] = ui;
        }

        foreach (SkillNodeUI child in nodes)
            foreach (string parentId in child.Node.parents)
                if (byId.TryGetValue(parentId, out SkillNodeUI parent)) Link(parent.Rect, child.Rect);

        Refresh();
    }

    public void Refresh()
    {
        if (progress == null) return;
        foreach (SkillNodeUI ui in nodes)
            ui.Set(progress.Rank(weapon, ui.Node.id), progress.IsUnlocked(weapon, ui.Node));
    }

    void OnClickNode(SkillNodeUI ui)
    {
        if (progress != null) progress.TrySpend(weapon, ui.Node.id);   // Changed -> SkillsTabUI refreshes
    }

    // Elbow from the parent's bottom-centre to the child's top-centre. Positions are
    // top-left anchored, y down (as authored), converted to anchoredPosition at the end.
    void Link(RectTransform parent, RectTransform child)
    {
        float px = Mathf.Round(parent.anchoredPosition.x + parent.sizeDelta.x / 2f);
        float py = -parent.anchoredPosition.y + parent.sizeDelta.y;
        float cx = Mathf.Round(child.anchoredPosition.x + child.sizeDelta.x / 2f);
        float cy = -child.anchoredPosition.y;
        float midY = Mathf.Round((py + cy) / 2f);

        Segment(px, py, lineWidth, midY - py);                                  // down from parent
        Segment(Mathf.Min(px, cx), midY, Mathf.Abs(cx - px) + lineWidth, lineWidth);   // across
        Segment(cx, midY, lineWidth, cy - midY);                                // down into child
    }

    void Segment(float x, float y, float w, float h)
    {
        if (w <= 0f || h <= 0f) return;
        Image line = Instantiate(lineTemplate, transform);
        line.gameObject.SetActive(true);
        line.color = lineColor;
        line.transform.SetAsFirstSibling();   // under the nodes
        line.rectTransform.anchoredPosition = new Vector2(x, -y);
        line.rectTransform.sizeDelta = new Vector2(w, h);
        lines.Add(line.gameObject);
    }

    void Clear()
    {
        foreach (SkillNodeUI ui in nodes) Destroy(ui.gameObject);
        foreach (GameObject l in lines) Destroy(l);
        nodes.Clear();
        lines.Clear();
    }
}
