using UnityEngine;

// Implemented by a swing-effect prefab's positioning component (SwordSwingEffectOrienter,
// AxeSwingEffectOrienter, ...) so Tool.SpawnSwingEffect can anchor+orient the effect
// without knowing which weapon type it belongs to. Each implementation is free to
// anchor however suits its art (continuous rotate-at-range, fixed per-direction
// offset, etc.) — this interface only standardizes the call Tool.cs makes.
public interface ISwingEffectAnchor
{
    void Orient(Vector2 aimDir, bool mirrorSweep, Transform anchor, float range);
}
