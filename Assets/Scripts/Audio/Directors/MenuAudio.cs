using UnityEngine;

// The game's own menus, heard (sound.md §10.9), from GameStateManager.StateChanged: the
// warm whoosh as Play lights the game from the main menu, the pause panel opening and
// closing. (The world ducking under the pause panel is the mixer's Paused snapshot, set
// by AudioSystem.) A director on the AudioSystem: it outlives the scene reload.
public class MenuAudio : MonoBehaviour
{
    GameState previous = GameState.MainMenu;

    void OnEnable() => GameStateManager.StateChanged += OnStateChanged;
    void OnDisable() => GameStateManager.StateChanged -= OnStateChanged;

    void OnStateChanged(GameState state)
    {
        if (state == GameState.Playing && previous == GameState.MainMenu) UiSfx.Play(UiSound.MainPlay);
        else if (state == GameState.Paused) UiSfx.Play(UiSound.Open);
        else if (state == GameState.Playing && previous == GameState.Paused) UiSfx.Play(UiSound.Close);
        previous = state;
    }
}
