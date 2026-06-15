using UnityEngine;

// Capability: this item is a tool/weapon with combat-ish stats. Data only for
// now (chopping/attacking behaviour comes later); sits next to Burnable as
// another item capability in the composition model.
public class Tool : MonoBehaviour
{
    public float damage = 1f;
    public float swingSpeed = 1f;
    public float range = 1f;        // how far in front of the wielder the strike lands
}
