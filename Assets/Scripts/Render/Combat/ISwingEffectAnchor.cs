using UnityEngine;

// Implemented by a swing-effect prefab's positioning component (SwingEffectOrienter)
// so Tool.SpawnSwingEffect can anchor+orient the effect without knowing which weapon
// type it belongs to. `reach` is the strike cone's radius: the art is fitted so its
// leading edge lands exactly on the cone's rim.
public interface ISwingEffectAnchor
{
    void Orient(Vector2 aimDir, bool mirrorSweep, Transform anchor, float reach);
}
