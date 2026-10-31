using System;
using UnityEngine;

public enum SkillNodeSize { Small, Big }

// Passive nodes are ranked stat upgrades (statsPerRank, summed into the player's
// CharacterStats by WeaponExperience). A Spell node is
// learned once (maxRank 1) and unlocks an active skill: its `skill` prefab
// (ActiveSkill), which the player carries from the start, dragged onto an action bar
// skill slot and cast with a hand holding this tree's weapon (see ActiveSkillSlots).
public enum SkillNodeKind { Passive, Spell }

// One node in a weapon's skill tree. Ranks are bought with that weapon's skill
// points; a node opens once every parent is at max rank. `position` is the node's
// top-left in reference pixels from the tree's origin on the Skills tab.
[Serializable]
public class SkillNode
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public SkillNodeKind kind = SkillNodeKind.Passive;
    public int maxRank = 1;
    public string[] parents = Array.Empty<string>();
    public SkillNodeSize size = SkillNodeSize.Small;
    public Vector2 position;
    [Tooltip("Drawn inside the node box (e.g. a spell's icon). Optional.")]
    public Sprite icon;
    [Tooltip("Passive: attributes/armor gained per rank bought.")]
    public StatBlock statsPerRank;
    [Tooltip("Spell: the skill's prefab (Prefabs/Skills) -- its behaviour, tuning and visuals. " +
             "The node gives it its name and icon.")]
    public ActiveSkill skill;
}

// A weapon's skill tree (Axe, Sword, ...). Pure data: WeaponExperience tracks the
// ranks bought, SkillTreeUI lays the nodes out.
[CreateAssetMenu(menuName = "Skills/Skill Tree")]
public class SkillTree : ScriptableObject
{
    public ToolKind weapon = ToolKind.Axe;
    public string displayName;
    public Sprite icon;
    public SkillNode[] nodes = Array.Empty<SkillNode>();

    public SkillNode Find(string id)
    {
        foreach (SkillNode n in nodes)
            if (n.id == id) return n;
        return null;
    }
}
