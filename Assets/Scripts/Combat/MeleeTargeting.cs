using System.Collections.Generic;
using UnityEngine;

// How a swing ranks the targets inside its cone when picking the primary.
//   AimAngle: smallest angle off the aim (the player's soft-targeting), then nearest.
//   Distance: nearest to the wielder (AI fallback, all-around spins).
public enum TargetScoring { AimAngle, Distance }

// One query for MeleeTargeting.Select.
public struct TargetQuery
{
    public GameObject owner;          // the wielder; never targeted
    public Vector2 origin;            // cone apex
    public Vector2 direction;         // unit aim
    public StrikeShape shape;
    public ToolKind kind;             // decides which targets the swing can damage
    public LayerMask targetLayers;    // hurtbox layers the swing can reach
    public LayerMask priorityLayers;  // hurtbox layers that outrank the rest (enemies over resources)
    public TargetScoring scoring;

    // A target to favor as primary: its score is lowered by preferredBias (degrees for
    // AimAngle, world units for Distance). float.PositiveInfinity forces it whenever
    // it's in the cone (AI aggro target, the player's committed target); a small bias
    // makes a live preview sticky. Tier still comes first, so a preferred resource
    // never beats an enemy in the cone.
    public GameObject preferred;
    public float preferredBias;
}

// A target picked by MeleeTargeting.Select. `point` is where the cone meets its hurt
// collider. `glancing` marks a target this swing can't damage, picked only because
// nothing damageable was in the cone, so the swing still connects for feedback
// (knockback, VFX, recoil) without dealing damage.
public struct StrikeTarget
{
    public Hurtbox hurtbox;
    public Vector2 point;
    public bool glancing;
}

// Picks who a melee swing hits: every Hurtbox whose hurt collider reaches into the
// aimed cone is a candidate, ranked by tier (damageable on a priority layer >
// damageable > can't be damaged), then by the query's scoring. The best is the
// primary; up to maxTargets - 1 secondaries follow, the damageable candidates
// nearest the primary. A glancing primary is struck alone. Static and allocation-free
// (shared buffers; main thread only), so the player's per-frame preview can call it.
public static class MeleeTargeting
{
    const int TierPriority = 0, TierDamageable = 1, TierGlancing = 2;

    struct Candidate
    {
        public Hurtbox hurtbox;
        public Vector2 point;
        public float score;
        public float distance;
        public int tier;
    }

    static readonly List<Collider2D> overlap = new();
    static readonly List<Candidate> candidates = new();

    // Fills `results` (cleared first) with the primary first, then the secondaries.
    public static void Select(in TargetQuery q, List<StrikeTarget> results)
    {
        results.Clear();
        if (q.shape.maxTargets <= 0 || q.shape.range <= 0f) return;

        Gather(q);
        if (candidates.Count == 0) return;

        int primary = 0;
        for (int i = 1; i < candidates.Count; i++)
            if (Ranks(candidates[i], candidates[primary])) primary = i;

        Candidate p = candidates[primary];
        bool glancing = p.tier == TierGlancing;
        results.Add(new StrikeTarget { hurtbox = p.hurtbox, point = p.point, glancing = glancing });
        if (glancing) return;

        // Secondaries: the damageable candidates nearest the primary (collateral of
        // the cleave), higher tiers first. A full circle (a spin) has no aim to cleave
        // along, so there it's simply the nearest to the wielder. Repeated min-scan:
        // the cap is tiny.
        bool radial = q.shape.arcAngle >= 360f;
        Vector2 at = p.hurtbox.Owner.transform.position;
        candidates.RemoveAt(primary);
        while (results.Count < q.shape.maxTargets)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate c = candidates[i];
                if (c.tier == TierGlancing) continue;
                float d = radial ? c.distance : ((Vector2)c.hurtbox.Owner.transform.position - at).sqrMagnitude;
                if (best < 0 || c.tier < candidates[best].tier || (c.tier == candidates[best].tier && d < bestDist))
                {
                    best = i;
                    bestDist = d;
                }
            }
            if (best < 0) break;
            results.Add(new StrikeTarget { hurtbox = candidates[best].hurtbox, point = candidates[best].point });
            candidates.RemoveAt(best);
        }
    }

    // a outranks b: tier, then score, then distance.
    static bool Ranks(in Candidate a, in Candidate b)
    {
        if (a.tier != b.tier) return a.tier < b.tier;
        if (a.score != b.score) return a.score < b.score;
        return a.distance < b.distance;
    }

    // Every live, hittable Hurtbox whose hurt collider reaches into the cone.
    static void Gather(in TargetQuery q)
    {
        candidates.Clear();

        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(q.targetLayers);
        overlap.Clear();
        Physics2D.OverlapCircle(q.origin, q.shape.range, filter, overlap);

        float half = q.shape.arcAngle * 0.5f;
        foreach (Collider2D col in overlap)
        {
            // only a hurt region counts (not an entity's blocker on the same layer)
            if (!Hurtbox.TryGet(col, out Hurtbox hb) || hb.Owner == q.owner || !hb.CanBeHit) continue;
            if (hb.Health == null || hb.Health.IsDead || Contains(hb.Health)) continue;
            if (!InCone(col, q, half, out Vector2 point, out float angle, out float distance)) continue;

            int tier = !hb.Accepts(q.kind) ? TierGlancing
                     : (q.priorityLayers & (1 << col.gameObject.layer)) != 0 ? TierPriority
                     : TierDamageable;

            float score = q.scoring == TargetScoring.AimAngle ? angle : distance;
            if (q.preferred != null && hb.Owner == q.preferred) score -= q.preferredBias;

            candidates.Add(new Candidate { hurtbox = hb, point = point, score = score, distance = distance, tier = tier });
        }
    }

    // One entry per entity, even if it has several hurtboxes.
    static bool Contains(Health health)
    {
        foreach (Candidate c in candidates)
            if (c.hurtbox.Health == health) return true;
        return false;
    }

    // Whether any part of `col` lies in the cone, tested against the collider's shape
    // rather than its pivot (a tree's hurtbox is a tall polygon). Two probes: the
    // collider's nearest point to the apex, and its nearest point to the aim line;
    // whichever sits closer to the aim (and still within reach) is the contact point.
    static bool InCone(Collider2D col, in TargetQuery q, float half,
                       out Vector2 point, out float angle, out float distance)
    {
        Vector2 o = q.origin;
        Vector2 dir = q.direction;
        float range = q.shape.range;

        point = col.ClosestPoint(o);
        Vector2 to = point - o;
        distance = to.magnitude;
        angle = 0f;
        if (distance > range) return false;
        if (distance < 1e-4f) return true;            // apex inside the collider: point blank

        angle = Vector2.Angle(dir, to);
        if (half >= 180f) return true;

        float t = Mathf.Clamp(Vector2.Dot((Vector2)col.bounds.center - o, dir), 0f, range);
        Vector2 onAim = col.ClosestPoint(o + dir * t);
        Vector2 toAim = onAim - o;
        float aimDist = toAim.magnitude;
        if (aimDist <= range)
        {
            float aimAngle = aimDist < 1e-4f ? 0f : Vector2.Angle(dir, toAim);
            if (aimAngle < angle)
            {
                point = onAim;
                angle = aimAngle;
            }
        }
        return angle <= half;
    }
}
