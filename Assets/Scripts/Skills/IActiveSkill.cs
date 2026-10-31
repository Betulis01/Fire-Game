using UnityEngine;

// An active weapon skill (spell), fired from an action bar skill slot (keys 1 / 2).
// Lives as a component on the player and is unlocked by the Spell node `NodeId` in
// the `Weapon` kind's skill tree; once learned it's dragged onto a slot, and casts
// with a drawn hand holding a weapon of that kind (see ActiveSkillSlots).
public interface IActiveSkill
{
    ToolKind Weapon { get; }
    string NodeId { get; }
    Sprite Icon { get; }
    string DisplayName { get; }
    float Cooldown01 { get; }   // 0 = ready, 1 = just used; drives the slot's overlay
    bool TryActivate(GameObject user, HandSide side);
}
