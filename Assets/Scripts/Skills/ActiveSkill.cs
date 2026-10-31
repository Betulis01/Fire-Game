using UnityEngine;

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
    }

    public abstract bool TryActivate(GameObject caster, HandSide side);

    // An Animation Event on the player's clip, by its function name.
    public virtual void OnAnimationEvent(string function) { }

    // Where strikes start: the body's centre (WeaponUse.Origin), else the player's feet.
    protected Vector2 Origin => weaponUse != null ? weaponUse.Origin : (Vector2)user.transform.position;

    protected bool IsHeld(GameObject item) =>
        hands.ActiveItem(HandSide.Left) == item || hands.ActiveItem(HandSide.Right) == item;
}
