using System.Collections.Generic;
using UnityEngine;

// The shape an aimed spell targets in, for its ground telegraph (SkillTelegraph) and
// the aim arrow: a cone of `arc` degrees out to `range` from the swing origin, or a
// lane `width` wide and `range` long.
public struct SkillAim
{
    public float range;
    public float arc;
    public bool lane;
    public float width;
}

// Base of every spell. A spell is a prefab of its own (Prefabs/Skills), linked from
// its Spell node in a weapon's SkillTree (SkillNode.skill): the prefab holds the
// behaviour, its tuning and anything it spawns (the flying axe); the node holds its
// id, name, icon and place in the tree. ActiveSkillSlots spawns one of each under
// the player at start and Binds it -- the tree gives its weapon, the player the parts
// it acts through (hands, animator, ...). A new spell is a subclass, a prefab with
// it, and a tree node pointing at the prefab.
//
// The player's clips play on the player, not here, so their Animation Events reach a
// spell through ActiveSkillSlots (OnAnimationEvent, by function name).
//
// An Aimed spell (Axe Throw) is armed by its key and cast by a left click
// (ActiveSkillSlots): while armed, the soft target (TargetPreview) runs its
// SelectTargets and the ground shows its TryGetAim shape. Spells that aren't aimed
// (Whirlwind) cast the moment their key is pressed.
public abstract class ActiveSkill : MonoBehaviour, IActiveSkill
{
    public ToolKind Weapon { get; private set; }
    public SkillNode Node { get; private set; }
    public string NodeId => Node?.id;
    public Sprite Icon => Node?.icon;
    public string DisplayName => Node?.displayName;
    public abstract float Cooldown01 { get; }

    protected GameObject user;          // the player casting it
    protected Hands hands;
    protected PlayerAnimator animator;
    protected WeaponUse weaponUse;
    protected TargetPreview preview;
    protected CharacterStats stats;
    WeaponExperience weaponXp;

    // The ranks bought in this spell's node (0 = not learned); a spell may grow with them.
    protected int Rank => weaponXp != null && Node != null ? weaponXp.Rank(Weapon, NodeId) : 0;

    public void Bind(GameObject owner, ToolKind weapon, SkillNode node)
    {
        user = owner;
        Weapon = weapon;
        Node = node;
        hands = owner.GetComponent<Hands>();
        animator = owner.GetComponent<PlayerAnimator>();
        weaponUse = owner.GetComponent<WeaponUse>();
        preview = owner.GetComponent<TargetPreview>();
        stats = owner.GetComponent<CharacterStats>();
        weaponXp = owner.GetComponent<WeaponExperience>();
    }

    public abstract bool TryActivate(GameObject caster, HandSide side);

    // Armed first and cast by a click, rather than cast on the key.
    public virtual bool Aimed => false;

    // A cast that's still choosing its target (Axe Throw's windup, before the axe
    // leaves the hand), on the hand CastSide: the soft target keeps following the spell
    // until then, so the cast lands on what was highlighted.
    public virtual bool AimLive => false;
    public virtual HandSide CastSide => HandSide.Left;

    // The shape this spell targets in when cast from `side`; false when it has none.
    public virtual bool TryGetAim(HandSide side, out SkillAim aim)
    {
        aim = default;
        return false;
    }

    // Fills `into` (cleared first) with what this spell would hit if cast from `side`
    // along `dir` now, primary first. `preferred` is favoured by `bias` the way
    // MeleeTargeting's TargetQuery favours it (stickiness, or a forced target).
    public virtual void SelectTargets(HandSide side, Vector2 dir, GameObject preferred, float bias, List<StrikeTarget> into)
    {
        into.Clear();
    }

    // An Animation Event on the player's clip, by its function name.
    public virtual void OnAnimationEvent(string function) { }

    // Where strikes start: the body's centre (WeaponUse.Origin), else the player's feet.
    protected Vector2 Origin => weaponUse != null ? weaponUse.Origin : (Vector2)user.transform.position;

    protected bool IsHeld(GameObject item) =>
        hands.ActiveItem(HandSide.Left) == item || hands.ActiveItem(HandSide.Right) == item;
}
