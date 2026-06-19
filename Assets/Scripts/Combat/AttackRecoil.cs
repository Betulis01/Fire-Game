using UnityEngine;

// Recoils the attacker backwards whenever one of its swings connects. Lives on the
// attacker next to its Knockback; as an IAttackReactor it's notified once per
// connecting swing with the hit, applying the weapon's selfKnockback (scaled by
// multiplier) as a push back from the strike point.
[RequireComponent(typeof(Knockback))]
public class AttackRecoil : MonoBehaviour, IAttackReactor
{
    [Tooltip("Scales the weapon's recoil for this attacker. 1 = exactly the weapon " +
             "value; lower for a heavier/sturdier body that resists its own swings.")]
    public float multiplier = 1f;

    Knockback knockback;

    void Awake() => knockback = GetComponent<Knockback>();

    public void OnDealtHit(in HitInfo hit)
    {
        if (knockback == null || hit.selfKnockback <= 0f) return;
        Vector2 recoilDir = (Vector2)transform.position - hit.point;   // back from the strike
        knockback.Apply(recoilDir, hit.selfKnockback * multiplier);
    }
}
