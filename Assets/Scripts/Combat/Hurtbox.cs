using System.Collections.Generic;
using UnityEngine;

// The vulnerable region of an entity. A melee strike or projectile that reaches this
// delivers a HitInfo here via TakeHit, which fans it out to the entity's IHitReactor
// components (Health, Knockback, ...). `owner` lets an attack ignore its own wielder.
// The hurt collider lives on a child (or the assigned hurtCollider), so no root
// collider is required.
public class Hurtbox : MonoBehaviour
{
    public Health health;          // defaults to the Health on this object/parents
    public GameObject owner;       // defaults to the health's GameObject
    public Collider2D hurtCollider; // the trigger region; defaults to first collider on this object

    public Health Health => health;
    public GameObject Owner => owner;
    public Collider2D HurtCollider => hurtCollider;

    // The entity's target highlight (TargetPreview shows it); null when it has no
    // Highlight art.
    public TargetHighlight Highlight { get; private set; }

    // The entity's stats; null for non-characters (trees, ore), which hits treat as
    // resources (DamageCalculator).
    public CharacterStats Stats { get; private set; }

    // hurt collider -> its Hurtbox, so targeting resolves an overlap without a
    // GetComponentInParent per collider (and ignores non-hurt colliders on the layer)
    static readonly Dictionary<Collider2D, Hurtbox> byCollider = new();

    IHitReactor[] reactors;        // cached effect handlers on the owner
    ToolDamageFilter damageFilter; // null = damageable by any tool

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (owner == null) owner = health != null ? health.gameObject : gameObject;
        // The hurt region lives on a dedicated child collider; an explicit
        // hurtCollider assignment always wins so a blocker on the root is never hit.
        if (hurtCollider == null) hurtCollider = FindHurtCollider();
        hurtCollider.isTrigger = true;   // only the hurtbox collider; never touch blockers

        reactors = owner.GetComponents<IHitReactor>();
        damageFilter = owner.GetComponent<ToolDamageFilter>();
        Stats = owner.GetComponent<CharacterStats>();
        Highlight = TargetHighlight.Attach(owner);
        byCollider[hurtCollider] = this;
    }

    // Unassigned: the "HurtboxArea" child by convention, else the first collider
    // found. Only this collider registers as hittable, so picking a sibling (an
    // interact or bump zone) would leave the entity unhittable.
    Collider2D FindHurtCollider()
    {
        Collider2D first = null;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
        {
            if (c.gameObject.name == "HurtboxArea") return c;
            if (first == null) first = c;
        }
        return first;
    }

    void OnDestroy()
    {
        if (hurtCollider != null && byCollider.TryGetValue(hurtCollider, out Hurtbox hb) && hb == this)
            byCollider.Remove(hurtCollider);
    }

    public static bool TryGet(Collider2D col, out Hurtbox hurtbox) => byCollider.TryGetValue(col, out hurtbox);

    // Whether a strike of this tool kind would damage the entity (its ToolDamageFilter).
    public bool Accepts(ToolKind kind) => damageFilter == null || damageFilter.Accepts(kind);

    // Deliver a landed hit to every reactor on this entity (damage, knockback, ...).
    public void TakeHit(in HitInfo hit)
    {
        foreach (IHitReactor reactor in reactors) reactor.OnHit(hit);
    }
}
