using UnityEngine;

// The player's body and status, heard (sound.md §3.2, §3.6, kill confirm in §4.1):
//   hurt        combat damage (a heavier gasp for a big chunk), burns, poison
//   cold        the shiver when warmth runs out, a quiet loop while freezing, a frost
//               tick as the cold eats health (throttled), a relieved exhale on warming up
//   heartbeat   a loop below lowHealth, louder and faster the lower it gets
//   death, heal, knockback
//   progress    the "+N" chime per kill (pitching up through a streak), level-ups,
//               a weapon levelling, food buffs starting and running out
// The hit's own impact is SurfaceAudio's; footsteps are FootstepAudio's.
public class PlayerAudio : MonoBehaviour, IKillReactor
{
    [Header("Body")]
    public SoundEvent hurt;
    public SoundEvent hurtHeavy;
    [Tooltip("Combat damage of at least this fraction of max health is a heavy hurt.")]
    [Range(0f, 1f)] public float heavyFraction = 0.25f;
    public SoundEvent hurtBurn;
    public SoundEvent death;
    public SoundEvent heal;
    public SoundEvent knockback;

    [Header("Cold")]
    public SoundEvent freezeStart;
    public SoundEvent freezingLoop;
    public SoundEvent coldTick;
    [Min(0f)] public float coldTickInterval = 1.5f;
    public SoundEvent warmRelief;

    [Header("Low health")]
    public SoundEvent heartbeatLoop;
    [Range(0f, 1f)] public float lowHealth = 0.25f;

    [Header("Progress")]
    public SoundEvent xpGain;
    [Tooltip("Kills within this many seconds of each other pitch the chime up a semitone each (up to six).")]
    [Min(0f)] public float streakWindow = 3f;
    public SoundEvent levelUp;
    public SoundEvent weaponLevelUp;
    public SoundEvent killConfirm;
    public SoundEvent buffStart;
    public SoundEvent buffEnd;

    Health health;
    PlayerTemperature temperature;
    Knockback push;
    PlayerExperience experience;
    WeaponExperience weaponXp;
    PlayerBuffs buffs;

    SoundHandle freezing, heartbeat;
    float nextColdTick, lastXp = float.NegativeInfinity;
    int streak;

    void Awake()
    {
        health = GetComponent<Health>();
        temperature = GetComponent<PlayerTemperature>();
        push = GetComponent<Knockback>();
        experience = GetComponent<PlayerExperience>();
        weaponXp = GetComponent<WeaponExperience>();
        buffs = GetComponent<PlayerBuffs>();
    }

    void OnEnable()
    {
        if (health != null) { health.Damaged += OnDamaged; health.Died += OnDied; health.Healed += OnHealed; }
        if (temperature != null) temperature.FreezingChanged += OnFreezing;
        if (push != null) push.Applied += OnPushed;
        if (experience != null) { experience.Awarded += OnXp; experience.LeveledUp += OnLevelUp; }
        if (weaponXp != null) weaponXp.LeveledUp += OnWeaponLevelUp;
        if (buffs != null) { buffs.Added += OnBuff; buffs.Expired += OnBuffEnd; }
    }

    void OnDisable()
    {
        if (health != null) { health.Damaged -= OnDamaged; health.Died -= OnDied; health.Healed -= OnHealed; }
        if (temperature != null) temperature.FreezingChanged -= OnFreezing;
        if (push != null) push.Applied -= OnPushed;
        if (experience != null) { experience.Awarded -= OnXp; experience.LeveledUp -= OnLevelUp; }
        if (weaponXp != null) weaponXp.LeveledUp -= OnWeaponLevelUp;
        if (buffs != null) { buffs.Added -= OnBuff; buffs.Expired -= OnBuffEnd; }
        freezing.Stop();
        heartbeat.Stop();
    }

    void Update()
    {
        if (health == null || heartbeatLoop == null) return;
        float ratio = health.Normalized;
        bool low = !health.IsDead && ratio < lowHealth;
        if (low && !heartbeat.IsValid) heartbeat = Sfx.PlayLoop(heartbeatLoop, transform, 0f);
        if (!heartbeat.IsValid) return;
        if (!low) { heartbeat.Stop(0.6f); return; }
        float t = lowHealth > 0f ? ratio / lowHealth : 0f;   // 1 at the threshold, 0 near death
        heartbeat.SetVolume(Mathf.Lerp(1f, 0.35f, t), 0.3f);
        heartbeat.SetPitch(Mathf.Lerp(1.3f, 1f, t));
    }

    void OnDamaged(DamageInfo info)
    {
        if (health.IsDead) return;   // the death sound covers the killing blow
        switch (info.type)
        {
            case DamageType.Environment:
                if (Time.unscaledTime < nextColdTick) return;
                nextColdTick = Time.unscaledTime + coldTickInterval;
                Sfx.Play(coldTick, transform);
                break;
            case DamageType.Fire:
                Sfx.Play(hurtBurn != null ? hurtBurn : hurt, transform);
                break;
            default:
                bool big = health.maxHealth > 0f && info.amount >= heavyFraction * health.maxHealth;
                Sfx.Play(big && hurtHeavy != null ? hurtHeavy : hurt, transform);
                break;
        }
    }

    void OnDied()
    {
        Sfx.Play(death, transform);
        freezing.Stop(0.3f);
        heartbeat.Stop(0.3f);
    }

    void OnHealed(float amount) => Sfx.Play(heal, transform);

    void OnPushed(Vector2 impulse) => Sfx.Play(knockback, transform);

    void OnFreezing(bool now)
    {
        if (now)
        {
            Sfx.Play(freezeStart, transform);
            if (!freezing.IsValid && freezingLoop != null) freezing = Sfx.PlayLoop(freezingLoop, transform);
        }
        else
        {
            freezing.Stop(1f);
            if (health == null || !health.IsDead) Sfx.Play(warmRelief, transform);
        }
    }

    void OnXp(int amount, Vector3 at)
    {
        streak = Time.unscaledTime - lastXp <= streakWindow ? Mathf.Min(streak + 1, 6) : 0;
        lastXp = Time.unscaledTime;
        Sfx.Play(xpGain, at, 1f, Mathf.Pow(2f, streak / 12f));
    }

    void OnLevelUp(int level) => Sfx.Play2D(levelUp);

    void OnWeaponLevelUp(ToolKind kind, int level) => Sfx.Play2D(weaponLevelUp);

    void OnBuff() => Sfx.Play(buffStart, transform);

    void OnBuffEnd() => Sfx.Play(buffEnd, transform);

    // A blow of the player's finishing an enemy (trees and ore die to the player too).
    public void OnKilled(Health victim, in DamageInfo killingBlow)
    {
        if (victim != null && victim.GetComponent<EnemyBrain>() != null)
            Sfx.Play(killConfirm, victim.transform.position);
    }
}
