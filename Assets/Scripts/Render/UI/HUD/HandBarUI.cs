using UnityEngine;

// Bottom hand bar: [left hand][action 1][action 2][right hand]. Mirrors the hands'
// contents (Hands.Changed), the selected hand (PlayerInteractor.ActiveHandChanged)
// and each hand's weapon skill (ActiveSkillSlots.Changed). The selected hand's
// frame grows, and the scroll-wheel and "Q Drop" hints move to sit beside / under
// it. Everything is placed explicitly (no layout groups) to stay pixel-exact.
public class HandBarUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] HandSlotUI leftHand;
    [SerializeField] HandSlotUI rightHand;
    [SerializeField] ActionSlotUI leftAction;
    [SerializeField] ActionSlotUI rightAction;

    [Header("Hints that follow the selected hand")]
    [SerializeField] RectTransform scrollHint;   // bottom-left pivot
    [SerializeField] RectTransform dropHint;     // bottom-left pivot (Q key + "Drop")
    [Tooltip("Gap between the selected frame's side and the scroll hint.")]
    [SerializeField] float scrollGap = 4f;
    [Tooltip("Scroll hint's height above the frame's bottom edge.")]
    [SerializeField] float scrollRaise = 15f;
    [Tooltip("Drop hint's x offset from the selected frame's left edge.")]
    [SerializeField] float dropOffsetX = 33f;

    Hands hands;
    PlayerInteractor interactor;
    ActiveSkillSlots skills;

    void OnEnable()
    {
        if (root == null) return;
        hands = root.Hands;
        interactor = root.Interactor;
        skills = root.Skills;

        if (hands != null) hands.Changed += Redraw;
        if (interactor != null) interactor.ActiveHandChanged += OnActiveHandChanged;
        if (skills != null) skills.Changed += RedrawSkills;

        Redraw();
        OnActiveHandChanged(interactor != null ? interactor.ActiveHand : HandSide.Left);
    }

    void OnDisable()
    {
        if (hands != null) hands.Changed -= Redraw;
        if (interactor != null) interactor.ActiveHandChanged -= OnActiveHandChanged;
        if (skills != null) skills.Changed -= RedrawSkills;
    }

    void OnActiveHandChanged(HandSide active)
    {
        leftHand.SetSelected(active == HandSide.Left);
        rightHand.SetSelected(active == HandSide.Right);

        RectTransform frame = (active == HandSide.Left ? leftHand : rightHand).Rect;
        Vector2 pos = frame.anchoredPosition;

        // scroll hint on the outer side of the selected hand
        float sx = active == HandSide.Left
            ? pos.x - scrollGap - scrollHint.sizeDelta.x
            : pos.x + frame.sizeDelta.x + scrollGap;
        scrollHint.anchoredPosition = new Vector2(sx, pos.y + scrollRaise);

        // "Q Drop" just under the selected hand
        dropHint.anchoredPosition = new Vector2(pos.x + dropOffsetX, dropHint.anchoredPosition.y);
    }

    void Redraw()
    {
        if (hands == null) return;
        leftHand.Set(UiPixel.IconOf(hands.Held(HandSide.Left)), hands.Count(HandSide.Left));
        rightHand.Set(UiPixel.IconOf(hands.Held(HandSide.Right)), hands.Count(HandSide.Right));
        RedrawSkills();
    }

    void RedrawSkills()
    {
        leftAction.SetSkill(skills != null ? skills.SkillFor(HandSide.Left) : null);
        rightAction.SetSkill(skills != null ? skills.SkillFor(HandSide.Right) : null);
    }
}
