using System;
using System.Collections.Generic;
using UnityEngine;

// Attacker-side reactor: per-weapon XP and skill points. Health tells it
// (IDamageDealtReactor) about every bit of damage the player actually dealt, with
// the striking tool's kind -- so an axe chopping a tree counts, while an axe
// glancing off stone (ToolDamageFilter) or a blocked 0-damage hit doesn't.
// Each weapon level grants skill points, spent on that weapon's SkillTree nodes.
// Read by the HUD (SkillsTabUI / SkillTreeUI).
public class WeaponExperience : MonoBehaviour, IDamageDealtReactor
{
    [Serializable]
    public struct XpRule
    {
        public ToolKind kind;
        public int xpPerHit;
    }

    [Tooltip("One tree per weapon kind; the Skills tab shows the tree for the selected weapon.")]
    [SerializeField] SkillTree[] trees = Array.Empty<SkillTree>();
    [Tooltip("Which weapon kinds earn XP, and how much per damaging hit.")]
    [SerializeField] XpRule[] rules = { new() { kind = ToolKind.Axe, xpPerHit = 1 } };
    [SerializeField] int xpPerLevel = 10;
    [SerializeField] int pointsPerLevel = 1;

    class Progress
    {
        public ExperienceTrack track;
        public int spent;
        public readonly Dictionary<string, int> ranks = new();
    }

    readonly Dictionary<ToolKind, Progress> progress = new();

    public event Action Changed;

    public void OnDealtDamage(Health victim, in DamageInfo info)
    {
        foreach (XpRule rule in rules)
            if (rule.kind == info.kind) Add(info.kind, rule.xpPerHit);
    }

    public void Add(ToolKind kind, int amount)
    {
        if (amount <= 0) return;
        Get(kind).track.Add(amount);
        Changed?.Invoke();
    }

    public int Level(ToolKind kind) => Get(kind).track.level;
    public int Xp(ToolKind kind) => Get(kind).track.xp;
    public int XpPerLevel => xpPerLevel;
    public float Normalized(ToolKind kind) => Get(kind).track.Normalized;
    public int Points(ToolKind kind) => Level(kind) * pointsPerLevel - Get(kind).spent;

    public int Rank(ToolKind kind, string nodeId) =>
        Get(kind).ranks.TryGetValue(nodeId, out int r) ? r : 0;

    public SkillTree TreeFor(ToolKind kind)
    {
        foreach (SkillTree t in trees)
            if (t != null && t.weapon == kind) return t;
        return null;
    }

    // A rank can be bought when there's a point to spend, the node isn't maxed,
    // and every parent node is maxed.
    public bool CanSpend(ToolKind kind, string nodeId)
    {
        SkillNode node = TreeFor(kind)?.Find(nodeId);
        if (node == null || Points(kind) <= 0 || Rank(kind, nodeId) >= node.maxRank) return false;
        return IsUnlocked(kind, node);
    }

    // Whether every parent is maxed (the node is reachable), regardless of points.
    public bool IsUnlocked(ToolKind kind, SkillNode node)
    {
        SkillTree tree = TreeFor(kind);
        foreach (string parentId in node.parents)
        {
            SkillNode parent = tree != null ? tree.Find(parentId) : null;
            if (parent != null && Rank(kind, parentId) < parent.maxRank) return false;
        }
        return true;
    }

    public bool TrySpend(ToolKind kind, string nodeId)
    {
        if (!CanSpend(kind, nodeId)) return false;
        Progress p = Get(kind);
        p.ranks[nodeId] = Rank(kind, nodeId) + 1;
        p.spent++;
        Changed?.Invoke();
        return true;
    }

    // Weapons start at level 0: the first point comes with the first weapon level.
    Progress Get(ToolKind kind)
    {
        if (!progress.TryGetValue(kind, out Progress p))
        {
            p = new Progress { track = new ExperienceTrack(0, xpPerLevel) };
            progress[kind] = p;
        }
        return p;
    }
}
