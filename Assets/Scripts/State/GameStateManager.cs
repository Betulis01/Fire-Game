using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { MainMenu, Playing, Paused, GameOver }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public GameState State { get; private set; }

    [Header("UI Panels (assign in Inspector)")]
    public GameObject mainMenuPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetState(GameState.MainMenu);
    }

    void Update()
    {
        // Esc toggles pause, but only while playing/paused
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }
    }

    public void SetState(GameState newState)
    {
        State = newState;

        // show only the panel for this state
        mainMenuPanel.SetActive(newState == GameState.MainMenu);
        pausePanel.SetActive(newState == GameState.Paused);
        gameOverPanel.SetActive(newState == GameState.GameOver);

        // freeze the world when not actively playing
        Time.timeScale = (newState == GameState.Playing) ? 1f : 0f;
    }

    // called by buttons
    public void PlayGame()    => SetState(GameState.Playing);
    public void ResumeGame()  => SetState(GameState.Playing);
    
    // Game Over → back to main menu (fresh)
    public void QuitToMenu()
    {
        Time.timeScale = 1f;                 // unfreeze BEFORE reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // scene reloads → manager's Start() runs → SetState(MainMenu)
    }
}