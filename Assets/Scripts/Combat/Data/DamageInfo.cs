using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float amount;
    public readonly DamageType type;
    public readonly Vector2? point;   // world-space contact point; null when not applicable (e.g. environmental)
    public readonly GameObject attacker;   // who dealt it; null for environmental damage (burn ticks, cold)
    public readonly ToolKind kind;         // the striking tool's kind; None when there was no tool

    public DamageInfo(float amount, DamageType type, Vector2? point = null,
                      GameObject attacker = null, ToolKind kind = ToolKind.None)
    {
        this.amount = amount;
        this.type = type;
        this.point = point;
        this.attacker = attacker;
        this.kind = kind;
    }
}
