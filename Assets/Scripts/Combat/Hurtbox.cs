using UnityEngine;

// The vulnerable region of an entity. A Hitbox that overlaps this routes damage
// to `health`. `owner` lets an attack ignore its own wielder.
[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    public Health health;        // defaults to the Health on this object/parents
    public GameObject owner;     // defaults to the health's GameObject

    public Health Health => health;
    public GameObject Owner => owner;

    void Awake()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (owner == null) owner = health != null ? health.gameObject : gameObject;
        GetComponent<Collider2D>().isTrigger = true;   // never blocks movement
    }
}
