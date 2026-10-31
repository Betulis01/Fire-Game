// A spell that takes a hand out of action for a while -- e.g. its axe is in flight
// (AxeThrowSkill). ActiveSkillSlots asks every spell; a locked hand can't swing
// (WeaponUse) or cast a spell, and isn't previewed.
public interface IHandLock
{
    bool Locks(HandSide side);
}
