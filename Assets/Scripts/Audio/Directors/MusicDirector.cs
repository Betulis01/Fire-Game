using System.Collections.Generic;
using UnityEngine;

// The music (sound.md §11.3), from AudioConfig.music:
//   base     one loop per state, crossfaded when it changes -- the main menu; day or night
//            while playing (and under the pause menu, which the Paused snapshot muffles);
//            the won loop after a victory; nothing after a death, which gets its sting
//   layers   while playing, each fades in while its condition holds and out after:
//              combat     the player's CombatState
//              freezing   the player has no warmth left
//              low fire   the nearest built campfire within lowFireRange burns low
// The player and the campfires are looked up again a few times a second (scenes reload).
// Loops start at the top (playhead 0), so a base and a layer recorded to the same tempo
// stay in step. Survives the scene reload, so the menu theme carries across it.
public class MusicDirector : MonoBehaviour
{
    SoundHandle baseLoop, combat, freezing, lowFire;
    SoundEvent baseEvent;
    GameState state = GameState.MainMenu;

    Transform player;
    CombatState playerCombat;
    PlayerTemperature temperature;
    Fuel campfire;
    float nextScan;
    readonly List<Station> stations = new();

    void OnEnable() => GameStateManager.StateChanged += OnStateChanged;

    void OnDisable()
    {
        GameStateManager.StateChanged -= OnStateChanged;
        baseLoop.Stop(1f);
        combat.Stop(1f);
        freezing.Stop(1f);
        lowFire.Stop(1f);
    }

    void OnStateChanged(GameState next)
    {
        MusicSounds m = Music;
        if (m != null && next == GameState.GameOver && state != GameState.GameOver)
        {
            SetBase(null, m);
            Sfx.Play2D(m.gameOver);
        }
        state = next;
    }

    static MusicSounds Music => AudioSystem.ActiveConfig != null ? AudioSystem.ActiveConfig.music : null;

    void Update()
    {
        MusicSounds m = Music;
        if (m == null) return;

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            Scan(m);
        }

        bool inGame = state is GameState.Playing or GameState.Paused;
        bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
        SoundEvent wanted = state switch
        {
            GameState.MainMenu => m.mainMenu,
            GameState.GameWon => m.gameWon,
            GameState.GameOver => null,
            _ => night ? m.night : m.day,
        };
        SetBase(wanted, m);

        SetLayer(ref combat, m.combat, inGame && playerCombat != null && playerCombat.InCombat, m);
        SetLayer(ref freezing, m.freezing, inGame && temperature != null && temperature.IsFreezing, m);
        SetLayer(ref lowFire, m.lowFire, inGame && campfire != null && campfire.fuelLevel <= m.lowFireAt, m);
    }

    void SetBase(SoundEvent e, MusicSounds m)
    {
        if (e == baseEvent && (e == null || baseLoop.IsValid)) return;
        baseLoop.Stop(m.baseCrossfade);
        baseEvent = e;
        baseLoop = default;
        if (e == null) return;
        baseLoop = Sfx.PlayLoop(e, null, 0f, 0f);
        baseLoop.SetVolume(1f, m.baseCrossfade);
    }

    static void SetLayer(ref SoundHandle h, SoundEvent e, bool want, MusicSounds m)
    {
        if (want)
        {
            if (!h.IsValid && e != null) h = Sfx.PlayLoop(e, null, 0f, 0f);
            h.SetVolume(1f, m.layerFadeIn);
        }
        else if (h.IsValid) h.Stop(m.layerFadeOut);
    }

    // the player, its combat and warmth, and the nearest built campfire
    void Scan(MusicSounds m)
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            player = p != null ? p.transform : null;
            playerCombat = p != null ? p.GetComponent<CombatState>() : null;
            temperature = p != null ? p.GetComponent<PlayerTemperature>() : null;
        }
        campfire = null;
        if (player == null) return;

        float best = m.lowFireRange;
        stations.Clear();
        stations.AddRange(FindObjectsByType<Station>(FindObjectsSortMode.None));
        foreach (Station s in stations)
        {
            if (s.type != StationType.Campfire || s.GetComponent<PlacementGhost>() != null) continue;
            float d = Vector2.Distance(s.transform.position, player.position);
            if (d > best || !s.TryGetComponent(out Fuel fuel)) continue;
            best = d;
            campfire = fuel;
        }
    }
}
