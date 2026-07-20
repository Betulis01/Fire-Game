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
    InputAction attackAction;
    InputAction interactLeftAction;
    InputAction interactRightAction;
    InputAction selectLeftAction;
    InputAction selectRightAction;
    InputAction journalAction;
    InputAction pauseAction;
    // Debug map
    InputAction toggleInvincibilityAction;
    InputAction toggleReadoutAction;
    InputAction toggleDayNightAction;

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
        attackAction = playerMap.FindAction("Attack", true);
        interactLeftAction = playerMap.FindAction("InteractLeft", true);
        interactRightAction = playerMap.FindAction("InteractRight", true);
        selectLeftAction = playerMap.FindAction("SelectLeft", true);
        selectRightAction = playerMap.FindAction("SelectRight", true);
        journalAction = playerMap.FindAction("Journal", true);

        globalMap = actions.FindActionMap("Global", throwIfNotFound: true);
        pauseAction = globalMap.FindAction("Pause", true);

        debugMap = actions.FindActionMap("Debug", throwIfNotFound: true);
        toggleInvincibilityAction = debugMap.FindAction("ToggleInvincibility", true);
        toggleReadoutAction = debugMap.FindAction("ToggleReadout", true);
        toggleDayNightAction = debugMap.FindAction("ToggleDayNight", true);
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
    public bool Attack => attackAction != null && attackAction.WasPressedThisFrame();
    public bool AttackHeld => attackAction != null && attackAction.IsPressed();
    public bool AttackReleased => attackAction != null && attackAction.WasReleasedThisFrame();
    public bool InteractLeft => interactLeftAction != null && interactLeftAction.WasPressedThisFrame();
    public bool InteractLeftHeld => interactLeftAction != null && interactLeftAction.IsPressed();
    public bool InteractLeftReleased => interactLeftAction != null && interactLeftAction.WasReleasedThisFrame();
    public bool InteractRight => interactRightAction != null && interactRightAction.WasPressedThisFrame();
    public bool SelectLeft => selectLeftAction != null && selectLeftAction.WasPressedThisFrame();
    public bool SelectRight => selectRightAction != null && selectRightAction.WasPressedThisFrame();
    public bool Journal => journalAction != null && journalAction.WasPressedThisFrame();
    public bool Pause => pauseAction != null && pauseAction.WasPressedThisFrame();
    public bool ToggleInvincibility => toggleInvincibilityAction != null && toggleInvincibilityAction.WasPressedThisFrame();
    public bool ToggleReadout => toggleReadoutAction != null && toggleReadoutAction.WasPressedThisFrame();
    public bool ToggleDayNight => toggleDayNightAction != null && toggleDayNightAction.WasPressedThisFrame();

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

    // A point along the aim direction from origin: at the pointer's distance for
    // mouse (clamped to maxDistance) or at maxDistance for gamepad. Used to place
    // ghosts (drop preview, build placement) under the cursor within a range.
    public Vector3 AimPoint(Vector2 origin, Camera cam, float maxDistance)
    {
        Vector2 aim = AimDirection(origin, cam);
        float dist = maxDistance;
        if (cam != null)
            dist = Mathf.Min((((Vector2)cam.ScreenToWorldPoint(PointerScreen)) - origin).magnitude, maxDistance);
        return origin + aim * dist;
    }
}
