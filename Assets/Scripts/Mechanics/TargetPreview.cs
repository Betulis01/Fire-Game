using System.Collections.Generic;
using UnityEngine;

// Live soft-target preview for the player's melee swings. Every frame it runs the
// same MeleeTargeting query a swing strikes with (including its lunge grace window,
// WeaponUse.SelectWithLunge) and highlights the result: the
// primary in primaryColor, secondaries in secondaryColor. While a swing is armed
// it previews that swing. Idle, each hand has its own button, so it previews the
// Main Hand -- or the Off Hand when only that one would actually damage something
// (axe + pickaxe facing a tree shows the axe).
//
// The primary is sticky: a rival must sit stickyDegrees closer to the aim to take
// over, so near-equal targets don't flicker under mouse jitter. WeaponUse commits a
// swing to Primary at its contact frame. Glancing targets (the weapon can't damage
// them) are struck for feedback but never highlighted.
[RequireComponent(typeof(WeaponUse))]
public class TargetPreview : MonoBehaviour
{
    [Tooltip("Vibrant gold (#FFCC00): reads as focus, not an error, on dark and light terrain.")]
    [SerializeField] Color primaryColor = new(1f, 0.8f, 0f, 1f);
    [Tooltip("Cool cyan (#40C8FF) at 50%: recedes visually so collateral targets don't add noise.")]
    [SerializeField] Color secondaryColor = new(0.2509804f, 0.78431374f, 1f, 0.5f);

    [Tooltip("Degrees closer to the aim a rival must be to take the primary slot " +
             "from the current primary.")]
    [SerializeField] float stickyDegrees = 5f;

    // The highlighted primary target's root; null when there is none.
    public GameObject Primary { get; private set; }

    WeaponUse weaponUse;
    readonly List<StrikeTarget> targets = new();
    readonly List<StrikeTarget> otherHand = new();
    List<TargetHighlight> shown = new();
    List<TargetHighlight> next = new();

    void Awake()
    {
        weaponUse = GetComponent<WeaponUse>();
    }

    void OnDisable()
    {
        targets.Clear();
        Refresh();
    }

    void LateUpdate()
    {
        bool playing = UserInput.Instance != null   // aim needs input; absent mid-reload
                       && (GameStateManager.Instance == null || GameStateManager.Instance.State == GameState.Playing);

        targets.Clear();
        if (playing)
        {
            if (weaponUse.IsSwingArmed)
            {
                if (weaponUse.TryGetArmedMeleePreview(out MeleeWeapon melee, out Tool tool, out StrikeShape shape, out Vector2 dir))
                    Select(melee, tool, shape, dir, targets);
            }
            else
            {
                HandSide first = HandSide.Left;
                PreviewHand(first, targets);
                if (!Damages(targets))
                {
                    PreviewHand(first == HandSide.Left ? HandSide.Right : HandSide.Left, otherHand);
                    if (Damages(otherHand))
                    {
                        targets.Clear();
                        targets.AddRange(otherHand);
                    }
                }
            }
        }

        Refresh();
    }

    void PreviewHand(HandSide side, List<StrikeTarget> into)
    {
        into.Clear();
        if (weaponUse.TryGetHandMeleePreview(side, out MeleeWeapon melee, out Tool tool, out StrikeShape shape, out Vector2 dir))
            Select(melee, tool, shape, dir, into);
    }

    // Through the lunge grace window, so a target the swing would step in to reach is
    // highlighted, with the secondaries it would cleave from where the step lands. A
    // swing that's stepping keeps its target highlighted however the step turns.
    void Select(MeleeWeapon melee, Tool tool, in StrikeShape shape, Vector2 dir, List<StrikeTarget> into)
    {
        GameObject stepping = weaponUse.StepTarget;
        weaponUse.SelectWithLunge(melee, tool, shape, dir, stepping != null ? stepping : Primary,
                                  stepping != null ? float.PositiveInfinity : stickyDegrees, into);
    }

    // Whether the swing would damage its primary (not just glance off it).
    static bool Damages(List<StrikeTarget> result) => result.Count > 0 && !result[0].glancing;

    // Show this frame's targets, hide the ones that dropped out.
    void Refresh()
    {
        next.Clear();
        Primary = null;

        for (int i = 0; i < targets.Count; i++)
        {
            StrikeTarget t = targets[i];
            if (t.glancing) break;
            if (i == 0) Primary = t.hurtbox.Owner;

            TargetHighlight h = t.hurtbox.Highlight;
            if (h == null) continue;
            h.SetTarget(i == 0 ? primaryColor : secondaryColor);
            next.Add(h);
        }

        foreach (TargetHighlight h in shown)
            if (h != null && !next.Contains(h)) h.ClearTarget();

        (shown, next) = (next, shown);
    }
}
