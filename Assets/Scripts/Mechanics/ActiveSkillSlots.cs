using System;
using System.Collections.Generic;
using UnityEngine;

// The player's spells and the two skill slots on the action bar (keys 1 / 2).
//
// Spells: at start one of every Spell node's skill prefab (SkillNode.skill, across
// WeaponExperience's trees) is spawned under a "Skills" child and bound to the player
// (ActiveSkill.Bind). Learning a node (WeaponExperience) is what lets it be slotted.
// The player's clips play on the player, so their Animation Events land here and are
// passed on to every spell (ActiveSkill.OnAnimationEvent) -- a new spell's clip event
// needs a one-line forward below.
//
// Slots: each holds a learned spell the player dragged there from a skill tree --
// nothing is assigned by itself, and a slot isn't tied to a hand: pressing its key
// casts the spell with whichever drawn hand holds that spell's weapon (the Main Hand
// first) and isn't locked (a spell's IHandLock).
//
// Aimed spells (ActiveSkill.Aimed: Axe Throw) arm instead: the key arms it (even mid-
// swing -- the swing plays out), the soft target and the ground telegraph follow the
// spell, and a left click casts it -- or the key again. A click during a swing casts
// the moment the swing ends. Right click cancels, and so does anything that takes the
// spell's hand away (a set swap, picking up a load, the weapon leaving the hand). The
// cast and cancel clicks are swallowed through their release, so they never also
// swing, charge or raise a shield. Spells that aren't aimed (Whirlwind) cast on the key.
// Runs before ItemUser (+100), so a click it claims never reaches the hands.
[RequireComponent(typeof(Hands))]
public class ActiveSkillSlots : MonoBehaviour
{
    public const int Count = 2;

    Hands hands;
    Loadout loadout;
    PlayerAnimator animator;
    WeaponExperience weaponXp;

    // The aimed spell waiting for its click, and the slot it was armed from.
    public ActiveSkill Armed { get; private set; }
    public event Action ArmedChanged;
    int armedSlot = -1;
    bool castQueued;   // clicked (or the key again): cast as soon as no swing plays
    readonly List<ActiveSkill> skills = new();
    readonly IActiveSkill[] slots = new IActiveSkill[Count];

    // raised when a slot's spell, a hand's contents or the learned spells change
    public event Action Changed;
    // A skill key that did nothing (empty slot, no hand for it, locked, on cooldown),
    // and a learned spell's cooldown running out -- for the audio layer.
    public event Action<int> CastFailed;
    public event Action<IActiveSkill> CooldownReady;
    readonly HashSet<ActiveSkill> cooling = new();

    void Awake()
    {
        hands = GetComponent<Hands>();
        loadout = GetComponent<Loadout>();
        animator = GetComponent<PlayerAnimator>();
        weaponXp = GetComponent<WeaponExperience>();
        SpawnSkills();
    }

    void SpawnSkills()
    {
        if (weaponXp == null) return;
        Transform root = new GameObject("Skills").transform;
        root.SetParent(transform, false);

        foreach (SkillTree tree in weaponXp.Trees)
        {
            if (tree == null) continue;
            foreach (SkillNode node in tree.nodes)
            {
                if (node.kind != SkillNodeKind.Spell || node.skill == null) continue;
                ActiveSkill skill = Instantiate(node.skill, root);
                skill.name = node.skill.name;
                skill.Bind(gameObject, tree.weapon, node);
                skills.Add(skill);
            }
        }
    }

    void OnEnable()
    {
        hands.Changed += RaiseChanged;
        if (weaponXp != null) weaponXp.Changed += RaiseChanged;
    }

    void OnDisable()
    {
        hands.Changed -= RaiseChanged;
        if (weaponXp != null) weaponXp.Changed -= RaiseChanged;
    }

    void RaiseChanged() => Changed?.Invoke();

    public IActiveSkill Skill(int slot) => slot >= 0 && slot < Count ? slots[slot] : null;

    // The player's spell for a skill tree node; null when there's none.
    public IActiveSkill Find(ToolKind weapon, string nodeId)
    {
        foreach (ActiveSkill skill in skills)
            if (skill.Weapon == weapon && skill.NodeId == nodeId) return skill;
        return null;
    }

    public bool IsLearned(IActiveSkill skill) =>
        skill != null && weaponXp != null && weaponXp.Rank(skill.Weapon, skill.NodeId) > 0;

    // Put a learned spell in a slot. Already in the other slot, the two swap.
    public bool Assign(int slot, IActiveSkill skill)
    {
        if (slot < 0 || slot >= Count || !IsLearned(skill)) return false;
        int from = Array.IndexOf(slots, skill);
        if (from == slot) return true;
        if (from >= 0) slots[from] = slots[slot];
        slots[slot] = skill;
        RaiseChanged();
        return true;
    }

    public void Clear(int slot)
    {
        if (slot < 0 || slot >= Count || slots[slot] == null) return;
        slots[slot] = null;
        RaiseChanged();
    }

    // The hand that would cast `skill` now: a drawn one holding its weapon, not locked.
    public bool TryGetCaster(IActiveSkill skill, out HandSide side)
    {
        foreach (HandSide s in new[] { HandSide.Left, HandSide.Right })
        {
            side = s;
            GameObject item = skill != null ? hands.ActiveItem(s) : null;
            if (item != null && item.TryGetComponent(out Tool tool) && tool.kind == skill.Weapon && !IsLocked(s))
                return true;
        }
        side = HandSide.Left;
        return false;
    }

    // The spell the soft target and the telegraph follow right now, and its hand: one
    // whose cast is still choosing its target (AimLive), else the armed one.
    public bool TryGetAiming(out ActiveSkill skill, out HandSide side)
    {
        foreach (ActiveSkill s in skills)
        {
            if (s == null || !s.AimLive) continue;
            skill = s;
            side = s.CastSide;
            return true;
        }
        skill = Armed;
        side = HandSide.Left;
        return Armed != null && TryGetCaster(Armed, out side);
    }

    // Whether a spell has a hand out of action (IHandLock: its axe is in flight).
    public bool IsLocked(HandSide side)
    {
        foreach (ActiveSkill skill in skills)
            if (skill is IHandLock l && l.Locks(side)) return true;
        return false;
    }

    // Animation Events on the player's clips, passed on to the spells.
    public void OnAttackHit() => Relay(nameof(OnAttackHit));
    public void OnWhirlwindHit() => Relay(nameof(OnWhirlwindHit));

    void Relay(string function)
    {
        foreach (ActiveSkill skill in skills)
            if (skill != null) skill.OnAnimationEvent(function);
    }

    void Update()
    {
        foreach (ActiveSkill skill in skills)
        {
            if (skill == null) continue;
            if (skill.Cooldown01 > 0f) cooling.Add(skill);
            else if (cooling.Remove(skill)) CooldownReady?.Invoke(skill);
        }

        if (Armed != null && !CanStayArmed()) Disarm();

        if (loadout != null && loadout.IsSwapping) return;   // the swap's beat
        if (UserInput.Instance.SkillPressed(HandSide.Left)) Press(0);
        if (UserInput.Instance.SkillPressed(HandSide.Right)) Press(1);
        if (Armed != null) UpdateArmed();
    }

    // A skill key: an aimed spell arms (or, already armed, casts); any other casts.
    void Press(int slot)
    {
        ActiveSkill skill = slots[slot] as ActiveSkill;
        if (skill == null)
        {
            CastFailed?.Invoke(slot);
            return;
        }
        if (!skill.Aimed)
        {
            Disarm();
            Cast(slot);
            return;
        }
        if (skill == Armed) castQueued = true;
        else if (CanArm(skill)) Arm(skill, slot);
        else CastFailed?.Invoke(slot);
    }

    // While armed: right click cancels, left click casts -- each press swallowed
    // through its release.
    void UpdateArmed()
    {
        UserInput input = UserInput.Instance;
        if (input.UsePressed(HandSide.Right))
        {
            Claim(HandSide.Right);
            Disarm();
            return;
        }
        if (input.UsePressed(HandSide.Left))
        {
            Claim(HandSide.Left);
            castQueued = true;
        }
        if (castQueued && (animator == null || !animator.IsAttacking)) CastArmed();
    }

    static void Claim(HandSide button)
    {
        UserInput.Instance.ConsumeUse(button);
        UserInput.Instance.SuppressUntilRelease(button);
    }

    // Off cooldown, with a drawn hand to cast it from; a swing may be playing.
    bool CanArm(ActiveSkill skill) =>
        skill.Cooldown01 <= 0f && !hands.IsCarrying && TryGetCaster(skill, out HandSide _);

    // The spell's hand is still there: not carrying, not swapping sets, the weapon still
    // drawn, and the game not over (a pause keeps it armed).
    bool CanStayArmed()
    {
        if (hands.IsCarrying || (loadout != null && loadout.IsSwapping)) return false;
        if (GameStateManager.Instance != null)
        {
            GameState state = GameStateManager.Instance.State;
            if (state != GameState.Playing && state != GameState.Paused) return false;
        }
        return TryGetCaster(Armed, out HandSide _);
    }

    void Arm(ActiveSkill skill, int slot)
    {
        Armed = skill;
        armedSlot = slot;
        castQueued = false;
        ArmedChanged?.Invoke();
    }

    public void Disarm()
    {
        castQueued = false;
        if (Armed == null) return;
        Armed = null;
        armedSlot = -1;
        ArmedChanged?.Invoke();
    }

    // Cast the armed spell from the hand that holds its weapon now. Hit or miss, the
    // arm is spent.
    void CastArmed()
    {
        ActiveSkill skill = Armed;
        int slot = armedSlot;
        bool cast = TryGetCaster(skill, out HandSide side) && skill.TryActivate(gameObject, side);
        Disarm();
        if (!cast) CastFailed?.Invoke(slot);
    }

    void Cast(int slot)
    {
        IActiveSkill skill = slots[slot];
        bool cast = !(skill is UnityEngine.Object o && o == null)   // a destroyed component behind the interface
                    && TryGetCaster(skill, out HandSide side) && skill.TryActivate(gameObject, side);
        if (!cast) CastFailed?.Invoke(slot);
    }
}
