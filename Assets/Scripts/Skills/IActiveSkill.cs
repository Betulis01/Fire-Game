using UnityEngine;

// An active weapon skill (spell), fired from a hand's action slot (keys 1 / 2).
// Lives as a component on the player and is unlocked by the Spell node `NodeId` in
// the `Weapon` kind's skill tree; while a hand holds a weapon of that kind, its
// action slot shows the skill (see ActiveSkillSlots).
public interface IActiveSkill
{
    ToolKind Weapon { get; }
    string NodeId { get; }
    Sprite Icon { get; }
    string DisplayName { get; }
    float Cooldown01 { get; }   // 0 = ready, 1 = just used; drives the slot's overlay
    bool TryActivate(GameObject user, HandSide side);
}
