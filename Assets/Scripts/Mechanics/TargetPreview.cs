using System.Collections.Generic;
using UnityEngine;

// Live soft-target preview for the player's melee swings. Every frame it runs the
// same MeleeTargeting query a swing strikes with (WeaponUse.TryGetMeleePreview: the
// armed swing, or the longer-reaching melee hand when idle) and highlights the
// result: the primary in primaryColor, secondaries in secondaryColor.
//
// The primary is sticky: a rival must sit stickyDegrees closer to the aim to take
// over, so near-equal targets don't flicker under mouse jitter. WeaponUse commits a
// swing to Primary at its contact frame. Glancing targets (the weapon can't damage
// them) are struck for feedback but never highlighted.
[RequireComponent(typeof(WeaponUse))]
public class TargetPreview : MonoBehaviour
{
    [SerializeField] Color primaryColor = new(1f, 0.55f, 0f, 1f);       // orange
    [SerializeField] Color secondaryColor = new(1f, 0f, 0f, 0.5f);      // 50% red

    [Tooltip("Degrees closer to the aim a rival must be to take the primary slot " +
             "from the current primary.")]
    [SerializeField] float stickyDegrees = 5f;

    // The highlighted primary target's root; null when there is none.
    public GameObject Primary { get; private set; }

    WeaponUse weaponUse;
    readonly List<StrikeTarget> targets = new();
    List<TargetHighlight> shown = new();
    List<TargetHighlight> next = new();

    void Awake() => weaponUse = GetComponent<WeaponUse>();

    void OnDisable()
    {
        targets.Clear();
        Refresh();
    }

    void LateUpdate()
    {
        bool playing = UserInput.Instance != null   // aim needs input; absent mid-reload
                       && (GameStateManager.Instance == null || GameStateManager.Instance.State == GameState.Playing);

        if (playing && weaponUse.TryGetMeleePreview(out MeleeWeapon melee, out Tool tool, out StrikeShape shape, out Vector2 dir))
            MeleeTargeting.Select(melee.Query(gameObject, weaponUse.Origin, dir, shape, tool.kind,
                                              TargetScoring.AimAngle, Primary, stickyDegrees), targets);
        else
            targets.Clear();

        Refresh();
    }

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
