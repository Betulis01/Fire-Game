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

    // Action maps. The Player map is gated by game state (SetGameplayInputEnabled);
    // Global (pause) and Debug stay live in every state.
    InputActionMap playerMap;
    InputActionMap globalMap;
    InputActionMap debugMap;

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

        playerMap = actions.FindActionMap("Player", throwIfNotFound: true);
        moveAction = playerMap.FindAction("Move", true);
        pointAction = playerMap.FindAction("Point", true);
        aimAction = playerMap.FindAction("Aim", true);
        attackLeftAction = playerMap.FindAction("AttackLeft", true);
        attackRightAction = playerMap.FindAction("AttackRight", true);
        interactLeftAction = playerMap.FindAction("InteractLeft", true);
        interactRightAction = playerMap.FindAction("InteractRight", true);
        craftAction = playerMap.FindAction("Craft", true);

        globalMap = actions.FindActionMap("Global", throwIfNotFound: true);
        pauseAction = globalMap.FindAction("Pause", true);

        debugMap = actions.FindActionMap("Debug", throwIfNotFound: true);
        toggleInvincibilityAction = debugMap.FindAction("ToggleInvincibility", true);
        toggleReadoutAction = debugMap.FindAction("ToggleReadout", true);
    }

    void OnEnable()
    {
        // Global (pause) and Debug are always live. The Player map stays disabled
        // until GameStateManager enables it for the Playing state, so menu/pause
        // clicks can never reach gameplay (attacks, interacts, movement).
        if (globalMap != null) globalMap.Enable();
        if (debugMap != null) debugMap.Enable();
    }

    void OnDisable()
    {
        if (actions != null) actions.Disable();
    }

    // Gameplay input (the Player map) exists only while actually playing. Called by
    // GameStateManager on every state change.
    public void SetGameplayInputEnabled(bool on)
    {
        if (playerMap == null) return;
        if (on) playerMap.Enable();
        else playerMap.Disable();
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
