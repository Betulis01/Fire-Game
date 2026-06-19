using UnityEngine;

// The vulnerable region of an entity. A Hitbox that overlaps this routes damage
// to `health`. `owner` lets an attack ignore its own wielder.
[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    public Health health;          // defaults to the Health on this object/parents
    public GameObject owner;       // defaults to the health's GameObject
    public Collider2D hurtCollider; // the trigger region; defaults to first collider on this object

    public Health Health => health;
    public GameObject Owner => owner;

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (owner == null) owner = health != null ? health.gameObject : gameObject;
        if (hurtCollider == null) hurtCollider = GetComponent<Collider2D>();
        hurtCollider.isTrigger = true;   // only the hurtbox collider; never touch blockers
    }
}
