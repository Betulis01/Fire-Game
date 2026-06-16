using UnityEngine;

// Wins the game when the day/night clock first reaches a target phase.
// Survive-to-noon by default: the clock starts in the afternoon, so the first
// crossing into Noon (after dusk, night, dawn, morning) is the win.
// Hooks DayNightCycle.PhaseChanged rather than polling.
public class WinCondition : MonoBehaviour
{
    [Tooltip("Reaching this phase while playing wins the game.")]
    public DayPhase goalPhase = DayPhase.Noon;

    // subscribe in Start (not OnEnable) so DayNightCycle.Awake has already set Instance
    void Start()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.PhaseChanged += OnPhaseChanged;
    }

    void OnDestroy()
    {
        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.PhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(DayPhase phase)
    {
        if (phase != goalPhase) return;
        if (GameStateManager.Instance.State != GameState.Playing) return;

        GameStateManager.Instance.GameWon();
    }
}
