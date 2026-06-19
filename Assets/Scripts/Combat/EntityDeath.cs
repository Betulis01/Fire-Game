using UnityEngine;

// Owns what happens to the entity itself when its Health is depleted: optionally
// fires a death-animation trigger, switches off collision and AI/movement so the
// corpse can't be hit or act, then destroys the object after destroyDelay.
// Loot is handled separately by DropOnDeath (both react to Health.Died).
[RequireComponent(typeof(Health))]
public class EntityDeath : MonoBehaviour
{
    [Tooltip("Seconds to wait after death before the object is destroyed, so a death " +
             "animation/sound can play. 0 = destroy immediately (e.g. trees).")]
    public float destroyDelay = 0f;

    [Tooltip("Optional Animator trigger fired on death (e.g. \"Die\"). Leave empty to skip.")]
    public string deathTrigger = "";

    [Tooltip("Behaviours switched off on death (AI, movement, attacking). The corpse goes limp.")]
    public Behaviour[] disableOnDeath;

    void Awake() => GetComponent<Health>().Died += Die;

    void Die()
    {
        // Stop the corpse interacting: no more collisions, no AI/movement.
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (Behaviour b in disableOnDeath) if (b != null) b.enabled = false;

        if (!string.IsNullOrEmpty(deathTrigger))
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger(deathTrigger);
        }

        Destroy(gameObject, destroyDelay);   // a delay of 0 destroys at end of this frame
    }
}
