using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Central input hub. Lives on the "Input Manager" GameObject and is the single
// source of truth for keybindings: all bindings come from the FireGameControls
// .inputactions asset (keyboard/mouse + gamepad). Gameplay scripts never touch
// the Input System directly; they poll UserInput.Instance instead, e.g.
//   if (UserInput.Instance.InteractLeft) ...
//   Vector2 move = UserInput.Instance.Move;
// Runs early (-100) so its per-frame state (pointer-over-UI latch, consumed
// presses) is settled before any gameplay script reads it.
//
// Mouse & keyboard and gamepad are never mixed: Mode follows whichever was used
// last (a real gamepad press or stick push, or a mouse move/click/key press). With
// a gamepad the cursor is hidden and ignored -- aim is the right stick, else the
// move direction, else the last aim, never wherever the cursor happens to rest --
// and with mouse & keyboard the aim stick is ignored, so a drifting stick can't
// steal the aim.
[DefaultExecutionOrder(-100)]
public class UserInput : MonoBehaviour
{
    public enum InputMode { MouseKeyboard, Gamepad }

    public static UserInput Instance { get; private set; }

    [Tooltip("FireGameControls input actions asset. Assign in the Inspector.")]
    [SerializeField] InputActionAsset actions;

    [Tooltip("Below this magnitude the gamepad aim stick is treated as idle, so " +
             "aiming keeps following the move direction / last aim.")]
    [SerializeField] float aimStickDeadzone = 0.35f;

    [Tooltip("A gamepad stick or trigger must pass this to switch into gamepad mode " +
             "(higher than the aim deadzone, so stick drift never switches).")]
    [SerializeField] float gamepadWakeThreshold = 0.5f;

    [Tooltip("Mouse movement (pixels in one frame) that switches back to mouse & keyboard.")]
    [SerializeField] float mouseWakeDistance = 4f;

    public InputMode Mode { get; private set; } = InputMode.MouseKeyboard;
    public bool UsingGamepad => Mode == InputMode.Gamepad;
    public event System.Action<InputMode> ModeChanged;

    Vector2 gamepadAim = Vector2.right;   // last stick (or move) direction in gamepad mode

    // Action maps. The Player map is gated by game state (SetGameplayInputEnabled);
    // Global (pause) and Debug stay live in every state.
    InputActionMap playerMap;
    InputActionMap globalMap;
    InputActionMap debugMap;

    // Player map
    InputAction moveAction;
    InputAction pointAction;
    InputAction aimAction;
    InputAction useLeftAction;      // LMB: whatever the left hand does (swing, eat, block)
    InputAction useRightAction;     // RMB: same for the right hand
    InputAction interactLeftAction;
    InputAction interactRightAction;
    InputAction skill1Action;       // left hand's action slot
    InputAction skill2Action;       // right hand's action slot
    InputAction switchHandAction;
    InputAction handScrollAction;
    InputAction menuAction;
    InputAction zoomInAction;       // Page Up
    InputAction zoomOutAction;      // Page Down
    InputAction pauseAction;
    // Debug map
    InputAction toggleInvincibilityAction;
    InputAction toggleReadoutAction;
    InputAction toggleDayNightAction;

    // Per hand (index = (int)HandSide): a mouse press that began over the HUD is
    // swallowed until its release, so clicking a menu button never swings or
    // blocks. consumedFrame lets one reader claim a press (e.g. PlacementGhost
    // placing) so later readers in the same frame don't also act on it.
    readonly bool[] suppressed = new bool[2];
    readonly int[] consumedFrame = { -1, -1 };
    bool pointerOverUI;

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
        useLeftAction = playerMap.FindAction("UseLeft", true);
        useRightAction = playerMap.FindAction("UseRight", true);
        interactLeftAction = playerMap.FindAction("InteractLeft", true);
        interactRightAction = playerMap.FindAction("InteractRight", true);
        skill1Action = playerMap.FindAction("Skill1", true);
        skill2Action = playerMap.FindAction("Skill2", true);
        switchHandAction = playerMap.FindAction("SwitchHand", true);
        handScrollAction = playerMap.FindAction("HandScroll", true);
        menuAction = playerMap.FindAction("Menu", true);
        zoomInAction = playerMap.FindAction("ZoomIn", true);
        zoomOutAction = playerMap.FindAction("ZoomOut", true);

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
        Cursor.visible = true;
    }

    // Gameplay input (the Player map) exists only while actually playing. Called by
    // GameStateManager on every state change.
    public void SetGameplayInputEnabled(bool on)
    {
        if (playerMap == null) return;
        if (on) playerMap.Enable();
        else playerMap.Disable();
    }

    void Update()
    {
        UpdateMode();
        if (UsingGamepad) UpdateGamepadAim();

        // a hidden cursor (gamepad mode) is over nothing
        pointerOverUI = !UsingGamepad && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        for (int i = 0; i < 2; i++)
        {
            InputAction use = UseAction((HandSide)i);
            if (use == null) continue;

            // a swallowed press stays swallowed through its release frame
            if (suppressed[i] && !use.IsPressed() && !use.WasReleasedThisFrame()) suppressed[i] = false;

            // only the mouse is gated by the cursor; a gamepad trigger isn't
            // "over" the HUD just because the cursor happens to rest on it
            if (use.WasPressedThisFrame())
                suppressed[i] = pointerOverUI && use.activeControl?.device is Mouse;
        }
    }

    // Switch to whichever device family was actually used this frame. Reads the
    // devices directly (not the actions), so it also works while the Player map is
    // off (main menu, pause).
    void UpdateMode()
    {
        if (Mode == InputMode.MouseKeyboard && GamepadUsed()) SetMode(InputMode.Gamepad);
        else if (Mode == InputMode.Gamepad && MouseKeyboardUsed()) SetMode(InputMode.MouseKeyboard);
    }

    void SetMode(InputMode mode)
    {
        Mode = mode;
        Cursor.visible = mode == InputMode.MouseKeyboard;
        ModeChanged?.Invoke(mode);
    }

    bool GamepadUsed()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null || !pad.wasUpdatedThisFrame) return false;

        float wake = gamepadWakeThreshold * gamepadWakeThreshold;
        if (pad.leftStick.ReadValue().sqrMagnitude > wake || pad.rightStick.ReadValue().sqrMagnitude > wake) return true;
        if (pad.leftTrigger.ReadValue() > gamepadWakeThreshold || pad.rightTrigger.ReadValue() > gamepadWakeThreshold) return true;

        if (pad != buttonsPad)
        {
            buttonsPad = pad;
            padButtons = new[]
            {
                pad.buttonSouth, pad.buttonEast, pad.buttonWest, pad.buttonNorth,
                pad.leftShoulder, pad.rightShoulder, pad.startButton, pad.selectButton,
                pad.leftStickButton, pad.rightStickButton,
                pad.dpad.up, pad.dpad.down, pad.dpad.left, pad.dpad.right,
            };
        }
        foreach (ButtonControl button in padButtons)
            if (button.wasPressedThisFrame) return true;
        return false;
    }

    Gamepad buttonsPad;          // the pad padButtons was built for
    ButtonControl[] padButtons;

    bool MouseKeyboardUsed()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.delta.ReadValue().sqrMagnitude > mouseWakeDistance * mouseWakeDistance) return true;
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame) return true;
            if (mouse.scroll.ReadValue().sqrMagnitude > 0.01f) return true;
        }
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
    }

    InputAction UseAction(HandSide side) => side == HandSide.Left ? useLeftAction : useRightAction;

    // --- Movement ---
    public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    // --- One-shot buttons (true only on the frame the control is pressed) ---
    // Use buttons, one per hand (LMB = left, RMB = right). All three read false for
    // a mouse press that started over the HUD; UsePressed also reads false once a
    // reader has claimed this frame's press with ConsumeUse.
    public bool UsePressed(HandSide side)
    {
        InputAction a = UseAction(side);
        return a != null && !suppressed[(int)side] && consumedFrame[(int)side] != Time.frameCount
            && a.WasPressedThisFrame();
    }
    public bool UseHeld(HandSide side)
    {
        InputAction a = UseAction(side);
        return a != null && !suppressed[(int)side] && a.IsPressed();
    }
    public bool UseReleased(HandSide side)
    {
        InputAction a = UseAction(side);
        return a != null && !suppressed[(int)side] && a.WasReleasedThisFrame();
    }
    public bool UseLeft => UsePressed(HandSide.Left);
    public bool UseRight => UsePressed(HandSide.Right);
    public void ConsumeUse(HandSide side) => consumedFrame[(int)side] = Time.frameCount;
    public bool InteractLeft => interactLeftAction != null && interactLeftAction.WasPressedThisFrame();
    public bool InteractLeftHeld => interactLeftAction != null && interactLeftAction.IsPressed();
    public bool InteractLeftReleased => interactLeftAction != null && interactLeftAction.WasReleasedThisFrame();
    public bool InteractRight => interactRightAction != null && interactRightAction.WasPressedThisFrame();
    public bool SkillPressed(HandSide side)
    {
        InputAction a = side == HandSide.Left ? skill1Action : skill2Action;
        return a != null && a.WasPressedThisFrame();
    }
    public bool SwitchHand => switchHandAction != null && switchHandAction.WasPressedThisFrame();
    public bool Menu => menuAction != null && menuAction.WasPressedThisFrame();
    public bool ZoomIn => zoomInAction != null && zoomInAction.WasPressedThisFrame();
    public bool ZoomOut => zoomOutAction != null && zoomOutAction.WasPressedThisFrame();

    // Scroll wheel this frame as a step: +1 (up), -1 (down) or 0.
    public int HandScrollStep
    {
        get
        {
            float y = handScrollAction != null ? handScrollAction.ReadValue<float>() : 0f;
            return y > 0.01f ? 1 : y < -0.01f ? -1 : 0;
        }
    }

    // Whether the mouse cursor is over a raycastable HUD element this frame.
    public bool PointerOverUI => pointerOverUI;
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

    // Aim direction from a world-space origin. Gamepad mode: the right stick while
    // it's past the deadzone, else the move direction, else the last of those (see
    // UpdateGamepadAim) -- never the hidden cursor. Mouse mode: from the origin
    // toward the cursor; the stick is ignored. Always a unit vector.
    public Vector2 AimDirection(Vector2 origin, Camera cam)
    {
        if (UsingGamepad) return gamepadAim;

        if (cam != null)
        {
            Vector2 world = cam.ScreenToWorldPoint(PointerScreen);
            Vector2 dir = world - origin;
            if (dir.sqrMagnitude > 0.0001f) return dir.normalized;
        }
        return Vector2.right;
    }

    // Once per frame, so every reader sees the same gamepad aim.
    void UpdateGamepadAim()
    {
        Vector2 stick = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;
        if (stick.sqrMagnitude >= aimStickDeadzone * aimStickDeadzone)
        {
            gamepadAim = stick.normalized;
            return;
        }
        Vector2 move = Move;
        if (move.sqrMagnitude >= aimStickDeadzone * aimStickDeadzone) gamepadAim = move.normalized;
    }

    // A point along the aim direction from origin: at the pointer's distance for
    // mouse (clamped to maxDistance) or at maxDistance for gamepad. Used to place
    // ghosts (drop preview, build placement) under the cursor within a range.
    public Vector3 AimPoint(Vector2 origin, Camera cam, float maxDistance)
    {
        Vector2 aim = AimDirection(origin, cam);
        float dist = maxDistance;
        if (!UsingGamepad && cam != null)
            dist = Mathf.Min((((Vector2)cam.ScreenToWorldPoint(PointerScreen)) - origin).magnitude, maxDistance);
        return origin + aim * dist;
    }
}
