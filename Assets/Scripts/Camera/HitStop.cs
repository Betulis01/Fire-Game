using System.Collections;
using UnityEngine;

// Brief global freeze on impact. Runs on Time.unscaledTime so it isn't affected by its
// own freeze; restores timeScale to 1 only if still Playing, so it can't undo a pause
// or game-over transition that happened mid-freeze.
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    Coroutine running;

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Stop(float seconds)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Freeze(seconds));
    }

    IEnumerator Freeze(float seconds)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        if (GameStateManager.Instance == null || GameStateManager.Instance.State == GameState.Playing)
            Time.timeScale = 1f;
        running = null;
    }
}
