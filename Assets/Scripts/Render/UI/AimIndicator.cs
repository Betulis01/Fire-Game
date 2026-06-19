using UnityEngine;

// A world-space arrow that orbits the player on the circle of the active weapon's
// reach, pointing where the player is aiming. It reads the same aim source as a real
// swing (UserInput.AimDirection from the swing origin), so it lands exactly on where
// an attack would. Shown only while a hand holds a real weapon (fists excluded) and
// the game is actually playing.
public class AimIndicator : MonoBehaviour
{
    [Tooltip("Hands to read held weapons (and their Tool.range) from.")]
    public Hands hands;

    [Tooltip("Swing origin to orbit around — the same point ToolUser strikes from.")]
    public Transform aimOrigin;

    [Tooltip("Camera for mouse aim. Defaults to Camera.main.")]
    public Camera cam;

    [Tooltip("The arrow sprite. Toggled on/off with weapon/state; defaults to one here.")]
    public SpriteRenderer arrow;

    [Tooltip("Degrees added so the sprite's drawn forward aligns with the aim (+X). " +
             "Arrow art pointing right = 0, up = -90.")]
    public float spriteAngleOffset = 0f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (arrow == null) arrow = GetComponent<SpriteRenderer>();

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

    // Largest reach among whatever the hands can swing, measured to the FURTHEST
    // point of the strike: Tool.range (strike center distance) + Hitbox.radius (how
    // far the strike circle extends past it). ActiveItem is the held item or the
    // default fists when empty, so bare hands count (fists are a Tool); a hand holding
    // a non-tool (e.g. wood) contributes nothing. Returns false only when neither hand
    // can swing anything — i.e. both hold non-tool items.
    bool TryActiveRange(out float range)
    {
        range = 0f;
        if (hands == null) return false;
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
            if (item.TryGetComponent(out Hitbox hitbox)) reach += hitbox.radius;
            range = Mathf.Max(range, reach);
            return true;
        }
        return false;
    }
}
