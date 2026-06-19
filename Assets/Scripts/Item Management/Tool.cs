using UnityEngine;

// Capability: this item is a tool/weapon with combat-ish stats. Data only for
// now (chopping/attacking behaviour comes later); sits next to Burnable as
// another item capability in the composition model.
public class Tool : MonoBehaviour
{

    public ToolKind kind = ToolKind.Fist;   // gates what this tool can damage
    public float damage = 1f;
    public float swingSpeed = 1f;
    public float range = 1f;        // how far in front of the wielder the strike lands

    [Tooltip("Shove dealt to a hit target, away from the attacker (needs a Knockback " +
             "component on the target).")]
    public float targetKnockback = 3f;

    [Tooltip("Recoil dealt back to the attacker on a connecting swing (applied by the " +
             "attacker's AttackRecoil, which can scale it).")]
    public float selfKnockback = 1f;

    // The hit-affecting stats bundled for a swing (damage/positioning live separately).
    public AttackData Attack => new AttackData(damage, kind, targetKnockback, selfKnockback);

}
