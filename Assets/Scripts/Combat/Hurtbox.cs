using UnityEngine;

// The vulnerable region of an entity. A Hitbox that overlaps this routes damage
// to `health`. `owner` lets an attack ignore its own wielder.
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

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (owner == null) owner = health != null ? health.gameObject : gameObject;
        // The hurt region lives on a dedicated child collider; an explicit
        // hurtCollider assignment always wins so a blocker on the root is never hit.
        if (hurtCollider == null) hurtCollider = GetComponentInChildren<Collider2D>();
        hurtCollider.isTrigger = true;   // only the hurtbox collider; never touch blockers
    }
}
