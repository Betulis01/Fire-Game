using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One node box in a skill tree: its icon (if the node has one, e.g. a spell) and
// "rank/max" in the corner. Dims while locked (a parent isn't maxed yet); a spell's
// icon stays faded until it's learned. Clicking spends a skill point on it.
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class SkillNodeUI : MonoBehaviour
{
    [SerializeField] TMP_Text rankText;
    [SerializeField] Image icon;
    [SerializeField] Vector2 iconBox = new(48f, 48f);
    [SerializeField] float lockedAlpha = 0.5f;
    [SerializeField] float unlearnedIconAlpha = 0.4f;

    public SkillNode Node { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    public event Action<SkillNodeUI> Clicked;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    public void Bind(SkillNode node)
    {
        Node = node;
        if (icon != null) UiPixel.FitIntegerScale(icon, node.icon, iconBox);
    }

    public void Set(int rank, bool unlocked)
    {
        rankText.text = rank + "/" + Node.maxRank;
        GetComponent<CanvasGroup>().alpha = unlocked ? 1f : lockedAlpha;
        if (icon != null && icon.enabled)
            icon.color = new Color(1f, 1f, 1f, rank > 0 ? 1f : unlearnedIconAlpha);
    }
}
