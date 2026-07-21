using UnityEngine;

// A world-space arrow that orbits the player on the circle of the next attack's
// reach, pointing where the player is aiming. It reads the same aim source as a real
// swing (UserInput.AimDirection from the swing origin) and asks WeaponUse which hand
// that swing would use, so it lands exactly on where an attack would. Hidden when
// that hand can't attack (e.g. it holds wood) or the game isn't actually playing.
public class AimIndicator : MonoBehaviour
{
    [Tooltip("Hands to read held weapons (and their Tool.range) from.")]
    public Hands hands;

    [Tooltip("Swing origin to orbit around — the same point WeaponUse strikes from.")]
    public Transform aimOrigin;

    [Tooltip("Camera for mouse aim. Defaults to Camera.main.")]
    public Camera cam;

    [Tooltip("The arrow sprite. Toggled on/off with weapon/state; defaults to one here.")]
    public SpriteRenderer arrow;

    [Tooltip("Degrees added so the sprite's drawn forward aligns with the aim (+X). " +
             "Arrow art pointing right = 0, up = -90.")]
    public float spriteAngleOffset = 0f;

    WeaponUse weaponUse;   // resolves which hand the next attack would use

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (arrow == null) arrow = GetComponent<SpriteRenderer>();
        if (hands != null) weaponUse = hands.GetComponent<WeaponUse>();

        // Detach from the player so we escape its SortingGroup; otherwise the group
        // forces us to sort with the player and our own Sorting Layer is ignored. We
        // already follow the player by world position each frame, so no parent needed.
        transform.SetParent(null, true);
    }

    void LateUpdate()
    {
        // Visible only when holding a real weapon and actively playing.
        bool armed = TryActiveRange(out float range);
        bool playing = GameStateManager.Instance == null
                       || GameStateManager.Instance.State == GameState.Playing;
        if (!armed || !playing)
        {
            if (arrow != null && arrow.enabled) arrow.enabled = false;
            return;
        }
        if (arrow != null && !arrow.enabled) arrow.enabled = true;

        // Pull the orbit in by the sprite's height so the arrow's tip (not its centre)
        // sits on the strike rim rather than overshooting it.
        if (arrow != null && arrow.sprite != null)
            range -= arrow.sprite.bounds.size.y * Mathf.Abs(arrow.transform.lossyScale.y);

        Vector2 origin = aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;
        Vector2 dir = UserInput.Instance.AimDirection(origin, cam);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.position = origin + dir * range;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
    }

    // Reach of the hand the next attack would use (WeaponUse.ResolveAttackHand),
    // measured to the FURTHEST point of the strike: Tool.range (strike center
    // distance) + Hitbox.radius (how far the strike circle extends past it) for
    // melee, or RangedWeapon.GetRange (live charge fraction lerped toward
    // chargedRange) for a bow. ActiveItem is the held item or the default fists
    // when empty, so bare hands count (fists are a Tool). Returns false when that
    // hand can't swing (it holds a non-tool, e.g. wood). Without a WeaponUse, falls
    // back to the larger reach of the two hands.
    bool TryActiveRange(out float range)
    {
        range = 0f;
        if (hands == null) return false;
        if (weaponUse != null) return Consider(weaponUse.ResolveAttackHand(), ref range);

        bool any = Consider(HandSide.Left, ref range);
        any |= Consider(HandSide.Right, ref range);
        return any;
    }

    bool Consider(HandSide side, ref float range)
    {
        GameObject item = hands.ActiveItem(side);
        if (item != null && item.TryGetComponent(out Tool tool))
        {
            float reach = tool.range;
            if (item.TryGetComponent(out Hitbox hitbox))
            {
                reach += hitbox.radius;
            }
            else if (item.TryGetComponent(out RangedWeapon ranged))
            {
                float chargeFraction = weaponUse != null ? weaponUse.ChargeProgress : 0f;
                reach = ranged.GetRange(chargeFraction, tool.range);
            }
            range = Mathf.Max(range, reach);
            return true;
        }
        return false;
    }
}
