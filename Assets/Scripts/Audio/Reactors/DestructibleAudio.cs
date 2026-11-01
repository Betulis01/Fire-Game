using System.Collections.Generic;
using UnityEngine;

// A prop's own reactions to being worked (sound.md §7.3, §9.1, §9.2, §6.6): a shake on
// every hit (needles and snow off a tree's branches -- glances too, like its HitAnimation),
// a deep creak once it's nearly gone, and its end: a tree felled, ore breaking, a log
// split into sticks, the big stone broken into rocks. The hit's own impact is
// SurfaceAudio's; the loot it drops is LootAudio's. Props that creak (the trees) are
// listed in Creakers, where the ambience finds a trunk to creak in the wind.
public class DestructibleAudio : MonoBehaviour, IHitReactor
{
    public static readonly List<Transform> Creakers = new();

    public SoundEvent shake;
    public SoundEvent creak;
    [Tooltip("The creak plays once, the first time health falls below this fraction.")]
    [Range(0f, 1f)] public float creakBelow = 0.3f;
    public SoundEvent destroyed;

    Health health;
    bool creaked;

    void Awake() => health = GetComponent<Health>();

    void OnEnable()
    {
        if (creak != null) Creakers.Add(transform);
        if (health == null) return;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDisable()
    {
        Creakers.Remove(transform);
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    public void OnHit(in HitInfo hit) => Sfx.Play(shake, transform.position);

    void OnDamaged(DamageInfo info)
    {
        if (creaked || health.IsDead || health.Normalized >= creakBelow) return;
        creaked = true;
        Sfx.Play(creak, transform.position);
    }

    // It's destroyed right after: play where it stood.
    void OnDied() => Sfx.Play(destroyed, transform.position);
}
