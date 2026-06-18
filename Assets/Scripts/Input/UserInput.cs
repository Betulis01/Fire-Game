using UnityEngine;
using UnityEngine.InputSystem;

// Central input hub. Lives on the "Input Manager" GameObject and is the single
// source of truth for keybindings: all bindings come from the FireGameControls
// .inputactions asset (keyboard/mouse + gamepad). Gameplay scripts never touch
// the Input System directly; they poll UserInput.Instance instead, e.g.
//   if (UserInput.Instance.InteractLeft) ...
//   Vector2 move = UserInput.Instance.Move;
public class UserInput : MonoBehaviour
{
    public static UserInput Instance { get; private set; }

    [Tooltip("FireGameControls input actions asset. Assign in the Inspector.")]
    [SerializeField] InputActionAsset actions;

    [Tooltip("Below this magnitude the gamepad aim stick is treated as idle, so " +
             "aiming falls back to the mouse pointer.")]
    [SerializeField] float aimStickDeadzone = 0.35f;

    // Player map
    InputAction moveAction;
    InputAction pointAction;
    InputAction aimAction;
    InputAction attackLeftAction;
    InputAction attackRightAction;
    InputAction interactLeftAction;
    InputAction interactRightAction;
    InputAction craftAction;
    InputAction pauseAction;
    // Debug map
    InputAction toggleInvincibilityAction;
    InputAction toggleReadoutAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (actions == null)
        {
            Debug.LogError("[UserInput] No InputActionAsset assigned; input will not work.");
            enabled = false;
            return;
        }

        InputActionMap player = actions.FindActionMap("Player", throwIfNotFound: true);
        moveAction = player.FindAction("Move", true);
        pointAction = player.FindAction("Point", true);
        aimAction = player.FindAction("Aim", true);
        attackLeftAction = player.FindAction("AttackLeft", true);
        attackRightAction = player.FindAction("AttackRight", true);
        interactLeftAction = player.FindAction("InteractLeft", true);
        interactRightAction = player.FindAction("InteractRight", true);
        craftAction = player.FindAction("Craft", true);
        pauseAction = player.FindAction("Pause", true);

        InputActionMap debug = actions.FindActionMap("Debug", throwIfNotFound: true);
        toggleInvincibilityAction = debug.FindAction("ToggleInvincibility", true);
        toggleReadoutAction = debug.FindAction("ToggleReadout", true);
    }

    void OnEnable()
    {
        if (actions != null) actions.Enable();
    }

    void OnDisable()
    {
        if (actions != null) actions.Disable();
    }

    // --- Movement ---
    public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    // --- One-shot buttons (true only on the frame the control is pressed) ---
    public bool AttackLeft => attackLeftAction != null && attackLeftAction.WasPressedThisFrame();
    public bool AttackRight => attackRightAction != null && attackRightAction.WasPressedThisFrame();
    public bool InteractLeft => interactLeftAction != null && interactLeftAction.WasPressedThisFrame();
    public bool InteractRight => interactRightAction != null && interactRightAction.WasPressedThisFrame();
    public bool Craft => craftAction != null && craftAction.WasPressedThisFrame();
    public bool Pause => pauseAction != null && pauseAction.WasPressedThisFrame();
    public bool ToggleInvincibility => toggleInvincibilityAction != null && toggleInvincibilityAction.WasPressedThisFrame();
    public bool ToggleReadout => toggleReadoutAction != null && toggleReadoutAction.WasPressedThisFrame();

    // --- Pointer / aim ---

    // Mouse/pointer position in screen space (0 if no pointer device is present).
    public Vector2 PointerScreen => pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;

    // Mouse/pointer position projected into the world through the given camera.
    public Vector3 PointerWorld(Camera cam)
    {
        if (cam == null) return Vector3.zero;
        return cam.ScreenToWorldPoint(PointerScreen);
    }

    // Aim direction from a world-space origin. Uses the gamepad right stick when it
    // is deflected past the deadzone; otherwise aims from the origin toward the
    // mouse cursor. Always returns a unit vector (falls back to Vector2.right).
    public Vector2 AimDirection(Vector2 origin, Camera cam)
    {
        Vector2 stick = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        if (stick.sqrMagnitude >= aimStickDeadzone * aimStickDeadzone)
            return stick.normalized;

        if (cam != null)
        {
            Vector2 world = cam.ScreenToWorldPoint(PointerScreen);
            Vector2 dir = world - origin;
            if (dir.sqrMagnitude > 0.0001f) return dir.normalized;
        }
        return Vector2.right;
    }
}
