using System;
using UnityEngine;

// The two action slots between the hands: slot 1 is the left hand's weapon skill,
// slot 2 the right hand's. A slot shows the learned spell (IActiveSkill on the
// player, unlocked by its Spell node in WeaponExperience) for the kind of weapon
// that hand holds, so it follows pick-ups/drops and learning for free. Keys 1 / 2
// fire them.
[RequireComponent(typeof(Hands))]
public class ActiveSkillSlots : MonoBehaviour
{
    Hands hands;
    WeaponExperience weaponXp;
    IActiveSkill[] skills;

    // raised when a hand's contents or the learned spells change
    public event Action Changed;

    void Awake()
    {
        hands = GetComponent<Hands>();
        weaponXp = GetComponent<WeaponExperience>();
        skills = GetComponents<IActiveSkill>();
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

    public IActiveSkill SkillFor(HandSide side)
    {
        GameObject item = hands.ActiveItem(side);
        if (item == null || weaponXp == null || !item.TryGetComponent(out Tool tool)) return null;

        foreach (IActiveSkill skill in skills)
            if (skill.Weapon == tool.kind && weaponXp.Rank(tool.kind, skill.NodeId) > 0) return skill;
        return null;
    }

    void Update()
    {
        if (UserInput.Instance.SkillPressed(HandSide.Left)) SkillFor(HandSide.Left)?.TryActivate(gameObject, HandSide.Left);
        if (UserInput.Instance.SkillPressed(HandSide.Right)) SkillFor(HandSide.Right)?.TryActivate(gameObject, HandSide.Right);
    }
}
