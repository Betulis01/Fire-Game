using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float amount;
    public readonly DamageType type;
    public readonly Vector2? point;   // world-space contact point; null when not applicable (e.g. environmental)

    public DamageInfo(float amount, DamageType type, Vector2? point = null)
    {
        this.amount = amount;
        this.type = type;
        this.point = point;
    }
}
