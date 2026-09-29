using UnityEngine;

// Held defensive item -- deliberately no Tool/Hitbox (ToolKind has no Shield entry:
// defensive, not a damage kind). Unlike other held items, which sort front/back by
// which hand holds them, a shield's draw order depends on which way the player is
// facing instead: the SE pose renders in front of the body, the NE pose behind it,
// regardless of hand. HeldItemSorter special-cases this component rather than
// running it through the Weapon/Item anchor tables (see its Apply).
[RequireComponent(typeof(SpriteRenderer))]
public class Shield : MonoBehaviour
{
    [Tooltip("Shown while facing SE (drawn in front of the body).")]
    public Sprite seSprite;

    [Tooltip("Shown while facing NE (drawn behind the body).")]
    public Sprite neSprite;

    [Tooltip("Fraction of incoming damage blocked while actively blocking with this shield (see ShieldBlock).")]
    [Range(0f, 1f)] public float blockReduction = 0.5f;

    [Tooltip("Added to the hand rig's local position while actively blocking, so the shield lifts in front of the body.")]
    public Vector2 raiseOffset = new(0.05f, 0.15f);

    SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    // Called by HeldItemSorter, which already tracks facing for the sort order --
    // no need for this to separately look up the player's facing too.
    public void SetFacing(bool se) => sr.sprite = se ? seSprite : neSprite;
}
