using UnityEngine;

// The player's weapons, heard (sound.md §4, §5's shared rows): listens to the swing
// pipeline (WeaponUse), the shield (ShieldBlock), the loadout (set swaps, stowing onto
// the back while carrying, drawing again) and the skill slots, and plays the swinging
// item's own sounds (its ToolAudio), else the shared defaults below. The impact of a hit
// isn't here: that's the victim's (SurfaceAudio).
//   charge   a rising loop while the attack is held past a beat (a bow's draw), the
//            ready ting once a heavy is guaranteed (the camera zooms in then too)
//   swing    the light or heavy whoosh on the swing frame
//   glance   a swing that only met things it can't damage
//   fire     a bow shot, louder the more it was drawn
[RequireComponent(typeof(WeaponUse))]
public class WeaponAudio : MonoBehaviour
{
    [Header("Defaults (when the tool's set leaves one empty)")]
    public SoundEvent chargeReady;
    public SoundEvent glance;
    public SoundEvent draw;
    public SoundEvent stow;

    [Header("Loadout and skills")]
    public SoundEvent loadoutSwap;
    public SoundEvent castDenied;
    public SoundEvent skillReady;

    [Tooltip("Seconds a press is held before the charge loop starts (a quick tap stays silent).")]
    [Min(0f)] public float chargeLoopDelay = 0.1f;

    WeaponUse weaponUse;
    ShieldBlock shield;
    Loadout loadout;
    Hands hands;
    ActiveSkillSlots skills;
    Tool charging;          // the armed swing's tool while its charge loop may play
    SoundHandle chargeLoop;

    void Awake()
    {
        weaponUse = GetComponent<WeaponUse>();
        shield = GetComponent<ShieldBlock>();
        loadout = GetComponent<Loadout>();
        hands = GetComponent<Hands>();
        skills = GetComponent<ActiveSkillSlots>();
    }

    void OnEnable()
    {
        weaponUse.SwingArmed += OnArmed;
        weaponUse.ChargeReady += OnChargeReady;
        weaponUse.Released += OnReleased;
        weaponUse.Swung += OnSwung;
        weaponUse.Landed += OnLanded;
        weaponUse.Fired += OnFired;
        if (shield != null) { shield.BlockingChanged += OnBlocking; shield.Blocked += OnBlocked; }
        if (loadout != null) loadout.Swapped += OnSwapped;
        if (hands != null) hands.CarryingChanged += OnCarrying;
        if (skills != null) { skills.CastFailed += OnCastFailed; skills.CooldownReady += OnSkillReady; }
        ItemEvents.Wielded += OnWielded;
    }

    void OnDisable()
    {
        weaponUse.SwingArmed -= OnArmed;
        weaponUse.ChargeReady -= OnChargeReady;
        weaponUse.Released -= OnReleased;
        weaponUse.Swung -= OnSwung;
        weaponUse.Landed -= OnLanded;
        weaponUse.Fired -= OnFired;
        if (shield != null) { shield.BlockingChanged -= OnBlocking; shield.Blocked -= OnBlocked; }
        if (loadout != null) loadout.Swapped -= OnSwapped;
        if (hands != null) hands.CarryingChanged -= OnCarrying;
        if (skills != null) { skills.CastFailed -= OnCastFailed; skills.CooldownReady -= OnSkillReady; }
        ItemEvents.Wielded -= OnWielded;
        StopCharge();
    }

    // The charge loop follows the hold: it starts after a beat and rises to full as the
    // charge reaches a heavy; any end of the charge stops it.
    void Update()
    {
        if (charging == null) return;
        if (!weaponUse.IsCharging) { StopCharge(); return; }
        if (weaponUse.ChargeElapsed < chargeLoopDelay) return;

        ToolSounds s = ToolAudio.Of(charging);
        if (s == null || s.chargeLoop == null) return;
        if (!chargeLoop.IsValid) chargeLoop = Sfx.PlayLoop(s.chargeLoop, transform, 0f);
        chargeLoop.SetVolume(Mathf.Lerp(0.3f, 1f, weaponUse.ChargeProgress), 0.05f);
    }

    void StopCharge()
    {
        charging = null;
        chargeLoop.Stop(0.08f);
    }

    void OnArmed(HandSide side, Tool tool) => charging = tool;

    void OnChargeReady(Tool tool) => Sfx.Play(Pick(ToolAudio.Of(tool)?.chargeReady, chargeReady), transform);

    void OnReleased(Tool tool, bool heavy, float charge) => StopCharge();

    void OnSwung(Tool tool, bool heavy)
    {
        StopCharge();
        ToolSounds s = ToolAudio.Of(tool);
        if (s == null) return;
        Sfx.Play(heavy ? Pick(s.swingHeavy, s.swing) : s.swing, transform);
    }

    // Only glances: every target it met is one this tool can't hurt (a fist on a tree).
    void OnLanded(Tool tool, StrikeResult result)
    {
        if (result.Missed || result.glances < result.hits) return;
        Sfx.Play(Pick(ToolAudio.Of(tool)?.glance, glance), transform);
    }

    void OnFired(Tool tool, float charge)
    {
        StopCharge();
        Sfx.Play(ToolAudio.Of(tool)?.fire, transform, Mathf.Lerp(0.6f, 1f, charge));
    }

    void OnBlocking(bool up)
    {
        ToolSounds s = ShieldSounds();
        if (s != null) Sfx.Play(up ? s.raise : s.lower, transform);
    }

    void OnBlocked(HitInfo hit) => Sfx.Play(ShieldSounds()?.block, hit.point);

    // the drawn shield's sounds (either hand)
    ToolSounds ShieldSounds()
    {
        if (hands == null) return null;
        foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
        {
            GameObject item = hands.ActiveItem(side);
            if (item != null && item.TryGetComponent(out Shield _)) return ToolAudio.Of(item);
        }
        return null;
    }

    void OnSwapped(int set)
    {
        Sfx.Play(loadoutSwap, transform);
        if (loadout != null) Sfx.Play(ToolAudio.Of(loadout.Drawn(HandSide.Left))?.draw, transform);
    }

    // picking up a load stows the loadout onto the back; putting it down draws it again
    void OnCarrying(bool carrying)
    {
        GameObject main = loadout != null ? loadout.Drawn(HandSide.Left) : null;
        ToolSounds s = ToolAudio.Of(main);
        Sfx.Play(carrying ? Pick(s?.stow, stow) : Pick(s?.draw, draw), transform);
    }

    void OnWielded(GameObject item) => Sfx.Play(Pick(ToolAudio.Of(item)?.draw, draw), transform);

    void OnCastFailed(int slot) => Sfx.Play2D(castDenied);

    void OnSkillReady(IActiveSkill skill) => Sfx.Play2D(skillReady);

    static SoundEvent Pick(SoundEvent preferred, SoundEvent fallback) => preferred != null ? preferred : fallback;
}
