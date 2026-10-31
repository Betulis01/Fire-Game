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
[RequireComponent(typeof(Hands))]
public class ActiveSkillSlots : MonoBehaviour
{
    public const int Count = 2;

    Hands hands;
    Loadout loadout;
    WeaponExperience weaponXp;
    readonly List<ActiveSkill> skills = new();
    readonly IActiveSkill[] slots = new IActiveSkill[Count];

    // raised when a slot's spell, a hand's contents or the learned spells change
    public event Action Changed;

    void Awake()
    {
        hands = GetComponent<Hands>();
        loadout = GetComponent<Loadout>();
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
        if (loadout != null && loadout.IsSwapping) return;   // the swap's beat
        if (UserInput.Instance.SkillPressed(HandSide.Left)) Cast(0);
        if (UserInput.Instance.SkillPressed(HandSide.Right)) Cast(1);
    }

    void Cast(int slot)
    {
        IActiveSkill skill = slots[slot];
        if (skill is UnityEngine.Object o && o == null) return;   // a destroyed component behind the interface
        if (TryGetCaster(skill, out HandSide side)) skill.TryActivate(gameObject, side);
    }
}
