using UnityEngine;

// Capability: this item fires a projectile instead of striking with a Hitbox. Sits
// beside Tool (stats) on a weapon prefab in place of Hitbox; data only, WeaponUse
// spawns the projectile. See Projectile for what the spawned arrow actually does.
public class RangedWeapon : MonoBehaviour
{
    public Projectile projectilePrefab;

    [Tooltip("Arrow speed on an immediate release (zero charge).")]
    public float projectileSpeed = 12f;

    [Tooltip("Arrow speed once fully charged (held for at least the tool's chargeTime). " +
             "Lerped with projectileSpeed by how charged the shot was. Defaults equal to " +
             "projectileSpeed, i.e. no scaling until tuned.")]
    public float chargedSpeed = 12f;

    [Tooltip("Arrow max travel distance once fully charged. 0 = same as the weapon's " +
             "base Tool.range (no range scaling until tuned).")]
    public float chargedRange = 0f;

    public float spawnOffset = 0.4f;   // distance in front of the wielder to spawn at

    public float GetSpeed(float chargeFraction) => Mathf.Lerp(projectileSpeed, chargedSpeed, chargeFraction);

    // baseRange is the weapon's Tool.range (0-charge distance); chargedRange (if set)
    // is the distance at full charge, lerped between the two by chargeFraction.
    public float GetRange(float chargeFraction, float baseRange)
    {
        float full = chargedRange > 0f ? chargedRange : baseRange;
        return Mathf.Lerp(baseRange, full, chargeFraction);
    }
}
