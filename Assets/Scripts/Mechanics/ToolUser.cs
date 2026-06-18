using UnityEngine;

// "Use the item in a hand" via the mouse: left button uses the left hand, right
// button the right. Using a hand swings a held weapon (Hitbox + Tool) toward the
// mouse cursor; the strike lands Tool.range in front and damages whatever Hurtbox
// it overlaps. Swing rate is limited by the tool's swingSpeed.
[RequireComponent(typeof(Hands))]
public class ToolUser : MonoBehaviour
{
    [Tooltip("Camera used to turn the mouse position into a world aim point. " +
             "Defaults to Camera.main if left empty.")]
    public Camera cam;

    [Tooltip("Point swings originate from (aim direction and strike center). " +
             "Place at the character's center; with a bottom pivot the transform " +
             "itself sits at the feet. Falls back to this transform if unset.")]
    public Transform aimOrigin;

    Hands hands;
    float leftReadyAt;
    float rightReadyAt;

    // World point swings originate from (aim direction + strike center). Uses the
    // assigned aimOrigin, falling back to this transform if none is set.
    public Vector2 Origin => aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;

    void Awake()
    {
        hands = GetComponent<Hands>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) UseHand(HandSide.Left);
        if (Input.GetMouseButtonDown(1)) UseHand(HandSide.Right);
    }

    void UseHand(HandSide side)
    {
        if (Time.time < ReadyAt(side)) return;

        GameObject held = hands.Held(side);
        if (held == null) return;

        Hitbox hitbox = held.GetComponent<Hitbox>();
        Tool tool = held.GetComponent<Tool>();
        if (hitbox == null || tool == null) return;   // only weapons can be swung

        Vector2 origin = Origin;
        Vector2 aim = AimDirection(origin);
        Vector2 center = origin + aim * tool.range;

        hitbox.Strike(tool.damage, gameObject, center);
        SetReadyAt(side, Time.time + 1f / Mathf.Max(0.01f, tool.swingSpeed));
    }

    // direction from the player toward the mouse cursor in world space
    Vector2 AimDirection(Vector2 origin)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector2.right;

        Vector2 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouse - origin;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    float ReadyAt(HandSide side) => side == HandSide.Left ? leftReadyAt : rightReadyAt;

    void SetReadyAt(HandSide side, float time)
    {
        if (side == HandSide.Left) leftReadyAt = time;
        else rightReadyAt = time;
    }
}
