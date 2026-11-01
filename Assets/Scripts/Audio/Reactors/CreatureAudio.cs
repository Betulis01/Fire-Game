using System.Collections.Generic;
using UnityEngine;

// A creature's voice and body (sound.md §6), from its CreatureVoice: rising out of the
// ground, idle barks while wandering, the aggro cry, the attack's windup / swing /
// contact, hurt (a burning variant), death, despawning at dawn, being shoved, and the
// chase / flee loops. Listens to EnemyAnimator, EnemyBrain, EnemyAttacker, Health,
// EnemyDespawner and Knockback on the same enemy; any of them may be missing (a boar
// has no attacker). Footsteps are FootstepAudio's, the hit's impact SurfaceAudio's.
//
// Idle barks go through a shared budget (AudioConfig.barkGap / barkGapPerKind): one in
// the whole world at a time, one per kind a few seconds apart, and only from creatures
// within hearing -- six wolves wandering together sound like a pack, not a choir.
public class CreatureAudio : MonoBehaviour
{
    public CreatureVoice voice;

    EnemyAnimator anim;
    EnemyBrain brain;
    EnemyAttacker attacker;
    Health health;
    EnemyDespawner despawner;
    Knockback push;
    Flammable flammable;

    SoundHandle loop;
    SoundEvent loopEvent;
    Vector2 lastPos;
    float moved;          // smoothed speed, for the chase loop
    float nextBark;

    static float lastBark = float.NegativeInfinity;
    static readonly Dictionary<CreatureVoice, float> lastBarkByKind = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lastBark = float.NegativeInfinity;
        lastBarkByKind.Clear();
    }

    void Awake()
    {
        anim = GetComponent<EnemyAnimator>();
        brain = GetComponent<EnemyBrain>();
        attacker = GetComponent<EnemyAttacker>();
        health = GetComponent<Health>();
        despawner = GetComponent<EnemyDespawner>();
        push = GetComponent<Knockback>();
        flammable = GetComponent<Flammable>();
    }

    void OnEnable()
    {
        if (anim != null) anim.SpawnStarted += OnSpawn;
        if (brain != null) brain.StateChanged += OnState;
        if (attacker != null) { attacker.SwingStarted += OnWindup; attacker.Swung += OnSwing; attacker.Struck += OnStruck; }
        if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; }
        if (despawner != null) despawner.Despawned += OnDespawned;
        if (push != null) push.Applied += OnPushed;
        lastPos = transform.position;
        ScheduleBark();
    }

    void OnDisable()
    {
        if (anim != null) anim.SpawnStarted -= OnSpawn;
        if (brain != null) brain.StateChanged -= OnState;
        if (attacker != null) { attacker.SwingStarted -= OnWindup; attacker.Swung -= OnSwing; attacker.Struck -= OnStruck; }
        if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; }
        if (despawner != null) despawner.Despawned -= OnDespawned;
        if (push != null) push.Applied -= OnPushed;
        SetLoop(null);
    }

    void Update()
    {
        if (voice == null || (health != null && health.IsDead)) return;

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            Vector2 pos = transform.position;
            moved = Mathf.Lerp(moved, (pos - lastPos).magnitude / dt, 1f - Mathf.Exp(-dt * 8f));
            lastPos = pos;
        }

        EnemyBrain.State state = brain != null ? brain.CurrentState : EnemyBrain.State.Wander;
        if (state == EnemyBrain.State.Flee) SetLoop(voice.fleeLoop);
        else if (state == EnemyBrain.State.Combat && moved > 0.1f) SetLoop(voice.chaseLoop);
        else SetLoop(null);

        if (state == EnemyBrain.State.Wander && Time.time >= nextBark && (anim == null || !anim.IsSpawning))
            TryBark();
    }

    void SetLoop(SoundEvent e)
    {
        if (e == loopEvent && (e == null || loop.IsValid)) return;
        loop.Stop(0.3f);
        loopEvent = e;
        loop = e != null ? Sfx.PlayLoop(e, transform) : default;
    }

    void ScheduleBark()
    {
        Vector2 range = voice != null ? voice.idleInterval : new Vector2(5f, 12f);
        nextBark = Time.time + Random.Range(range.x, Mathf.Max(range.x, range.y));
    }

    void TryBark()
    {
        AudioConfig config = AudioSystem.ActiveConfig;
        if (voice.idle == null || config == null) { ScheduleBark(); return; }

        // out of hearing: don't spend the budget on it
        if (AudioSpace.RadiiAway(config, transform.position, voice.idle.rangeScale) >= 1f) { ScheduleBark(); return; }

        float now = Time.unscaledTime;
        bool kindReady = !lastBarkByKind.TryGetValue(voice, out float last) || now - last >= config.barkGapPerKind;
        if (now - lastBark < config.barkGap || !kindReady)
        {
            nextBark = Time.time + Random.Range(0.5f, 1.5f);   // try again soon
            return;
        }

        lastBark = now;
        lastBarkByKind[voice] = now;
        Sfx.Play(voice.idle, transform);
        ScheduleBark();
    }

    void OnSpawn() { if (voice != null) Sfx.Play(voice.spawn, transform.position); }

    void OnState(EnemyBrain.State from, EnemyBrain.State to)
    {
        if (voice == null) return;
        if (to == EnemyBrain.State.Combat) Sfx.Play(voice.aggro, transform);
        else if (to == EnemyBrain.State.Flee) Sfx.Play(voice.flee, transform);
        else if (to == EnemyBrain.State.Wander) ScheduleBark();
    }

    void OnWindup() { if (voice != null) Sfx.Play(voice.attackWindup, transform); }
    void OnSwing() { if (voice != null) Sfx.Play(voice.attackSwing, transform); }
    void OnStruck(bool landed) { if (voice != null) Sfx.Play(voice.attackHit, transform); }

    void OnDamaged(DamageInfo info)
    {
        if (voice == null || health.IsDead) return;   // the death covers the killing blow
        bool burning = flammable != null && flammable.IsBurning && voice.hurtBurning != null;
        Sfx.Play(burning ? voice.hurtBurning : voice.hurt, transform);
    }

    void OnDied()
    {
        SetLoop(null);
        if (voice != null) Sfx.Play(voice.death, transform.position);
    }

    // It's destroyed right after: play where it stood.
    void OnDespawned() { if (voice != null) Sfx.Play(voice.despawn, transform.position); }

    void OnPushed(Vector2 impulse) { if (voice != null) Sfx.Play(voice.knockback, transform); }
}
