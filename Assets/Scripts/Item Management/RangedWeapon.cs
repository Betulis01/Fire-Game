using UnityEngine;

// Capability: this item fires a projectile instead of striking with a Hitbox. Sits
// beside Tool (stats) on a weapon prefab in place of Hitbox; data only, ToolUser
// spawns the projectile. See Projectile for what the spawned arrow actually does.
public class RangedWeapon : MonoBehaviour
{
    public Projectile projectilePrefab;
    public float projectileSpeed = 12f;
    public float spawnOffset = 0.4f;   // distance in front of the wielder to spawn at
}
