using UnityEngine;

// The vulnerable region of an entity. A Hitbox that overlaps this delivers a HitInfo
// here via TakeHit, which fans it out to the entity's IHitReactor components (Health,
// Knockback, ...). `owner` lets an attack ignore its own wielder. The hurt collider
// lives on a child (or the assigned hurtCollider), so no root collider is required.
public class Hurtbox : MonoBehaviour
{
    public Health health;          // defaults to the Health on this object/parents
    public GameObject owner;       // defaults to the health's GameObject
    public Collider2D hurtCollider; // the trigger region; defaults to first collider on this object

    public Health Health => health;
    public GameObject Owner => owner;
    public Collider2D HurtCollider => hurtCollider;

    IHitReactor[] reactors;        // cached effect handlers on the owner

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (owner == null) owner = health != null ? health.gameObject : gameObject;
        // The hurt region lives on a dedicated child collider; an explicit
        // hurtCollider assignment always wins so a blocker on the root is never hit.
        if (hurtCollider == null) hurtCollider = GetComponentInChildren<Collider2D>();
        hurtCollider.isTrigger = true;   // only the hurtbox collider; never touch blockers

        reactors = owner.GetComponents<IHitReactor>();
    }

    // Deliver a landed hit to every reactor on this entity (damage, knockback, ...).
    public void TakeHit(in HitInfo hit)
    {
        foreach (IHitReactor reactor in reactors) reactor.OnHit(hit);
    }
}
