using UnityEngine;

// Root of the screen HUD (stats, hand bar, F menu). Finds the player once and hands
// its components to the HUD widgets, so none of them search the scene themselves.
// Runs early so the widgets can read it from their own Awake/OnEnable. The whole
// HUD is hidden outside play (main menu, game over, won) via its CanvasGroup;
// it stays up under the pause panel.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CanvasGroup))]
public class HudRoot : MonoBehaviour
{
    public GameObject Player { get; private set; }
    public Hands Hands { get; private set; }
    public PlayerInteractor Interactor { get; private set; }
    public Health Health { get; private set; }
    public PlayerTemperature Temperature { get; private set; }
    public PlayerExperience Experience { get; private set; }
    public WeaponExperience WeaponExperience { get; private set; }
    public Equipment Equipment { get; private set; }
    public CraftingController Crafting { get; private set; }
    public ActiveSkillSlots Skills { get; private set; }

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();

        Player = GameObject.FindGameObjectWithTag("Player");
        if (Player == null)
        {
            Debug.LogWarning("[HudRoot] No GameObject tagged Player; the HUD stays empty.", this);
            return;
        }

        Hands = Player.GetComponent<Hands>();
        Interactor = Player.GetComponent<PlayerInteractor>();
        Health = Player.GetComponent<Health>();
        Temperature = Player.GetComponent<PlayerTemperature>();
        Experience = Player.GetComponent<PlayerExperience>();
        WeaponExperience = Player.GetComponent<WeaponExperience>();
        Equipment = Player.GetComponent<Equipment>();
        Crafting = Player.GetComponent<CraftingController>();
        Skills = Player.GetComponent<ActiveSkillSlots>();
    }

    void Update()
    {
        GameStateManager gsm = GameStateManager.Instance;
        bool visible = gsm == null || gsm.State == GameState.Playing || gsm.State == GameState.Paused;
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
    }
}
