using UnityEngine;
using UnityEngine.EventSystems;

// The F menu under the player stats: Player (equipment, loadouts and the bag),
// Crafting and Skills tabs. F toggles it -- or, while a placement ghost is following
// the cursor, cancels that instead. Esc closes it through GameStateManager
// (TryClose), so Esc never both closes the menu and pauses. Always opens on the
// first tab (Player).
public class HudMenu : MonoBehaviour
{
    public static HudMenu Instance { get; private set; }

    [SerializeField] GameObject panel;          // tabs + pages; hidden while closed
    [SerializeField] TabButtonUI[] tabs;        // Player, Crafting, Skills
    [SerializeField] GameObject[] pages;        // same order as tabs

    int current;

    public bool IsOpen => panel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].Clicked += () => Select(index);
        }
        Select(0);
        panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!UserInput.Instance.Menu) return;
        if (PlacementGhost.CancelActive()) return;
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        panel.SetActive(true);
        Select(0);
    }

    public void Close()
    {
        panel.SetActive(false);
        // a clicked button stays "selected" otherwise, and Space/Enter would re-press it
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // Close if open; returns whether it was (Esc handling).
    public bool TryClose()
    {
        if (!IsOpen) return false;
        Close();
        return true;
    }

    public void Select(int index)
    {
        current = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == current);
            tabs[i].SetSelected(i == current);
        }
    }
}
