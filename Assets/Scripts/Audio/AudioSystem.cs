using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// The audio layer's engine room (SoundSystemPlan.md §3.5). It creates itself before
// the first scene loads and persists, so music and UI survive "Quit to menu" (a scene
// reload) and no scene needs any setup. The game never talks to it directly -- it
// calls Sfx -- and only this class touches AudioSource.
//
// One-shots: a fixed pool of voices (one AudioSource each, made once, so playing
// allocates nothing). A start passes, in order: the event's cooldown, the frame's
// start budget, the cull (a World sound too quiet where it plays isn't started), the
// event's voice cap (steal its oldest / quietest, or refuse), and a free voice --
// with none free, the lowest-priority, then quietest, then oldest voice is stolen,
// or the new one is dropped when everything playing matters more.
//
// Loops are virtual: a loop is a record that holds a pooled source only while it's
// within hearing (and gives it back beyond loopReleaseRadii), advancing its own
// playhead meanwhile, so a dozen fires or torches cost voices only when you're near.
//
// All timing is unscaled: pause, the menus and HitStop set timeScale to 0. Pausing the
// game pauses the listener (AudioListener.pause), which every source honours except
// ignorePause ones (UI, music); the mixer's snapshot follows GameState. A scene being
// unloaded takes its World voices and loops with it -- including any started by its
// objects' OnDisable on the way out, which would otherwise burst out on a reload.
//
// Every AudioSource is 2D (spatialBlend 0); AudioSpace computes gain and pan from the
// camera's view. The one AudioListener lives here (where it sits doesn't matter).
public class AudioSystem : MonoBehaviour
{
    public static AudioSystem Instance { get; private set; }
    static bool quitting;

    // The running config, for the audio reactors (null outside play mode).
    public static AudioConfig ActiveConfig => Instance != null ? Instance.config : null;

    // One playing (or, for a loop, wanted) sound. A SoundHandle points at one by
    // generation, so a handle to a finished or stolen voice simply goes stale.
    internal sealed class Voice
    {
        public int generation;
        public bool active, loop, screen, stopping;
        public SoundEvent ev;
        public AudioClip clip;
        public AudioSource source;      // one-shots always; loops only while audible
        public Transform follow;
        public bool hasFollow;
        public Vector2 point;
        public float baseGain;          // the event's level and jitter x the caller's volume
        public float basePitch;         // jitter x the caller's pitch
        public float pitchScale = 1f;
        public float userGain = 1f, userGainTarget = 1f, userGainSpeed;
        public float fade = 1f, fadeSpeed;
        public float remaining;         // one-shots: seconds of audio left
        public float playhead;          // loops: seconds into the clip
        public float started;           // unscaled
        public float gain;              // last computed output level (stealing, debug)

        public void SetUserGain(float target, float fadeTime)
        {
            userGainTarget = Mathf.Max(0f, target);
            if (fadeTime <= 0f) { userGain = userGainTarget; userGainSpeed = 0f; }
            else userGainSpeed = Mathf.Abs(userGainTarget - userGain) / fadeTime;
        }

        public void BeginStop(float fadeTime)
        {
            stopping = true;
            fadeSpeed = fadeTime <= 0f ? float.PositiveInfinity : 1f / fadeTime;
        }
    }

    // Everything that changes while an event plays. Kept here, never on the asset.
    sealed class EventState
    {
        public float lastStart = float.NegativeInfinity;
        public int[] bag;
        public int bagIndex, last = -1, sequence;
        public readonly List<Voice> live = new();
    }

    AudioConfig config;
    Voice[] oneShots;
    readonly Stack<Voice> freeOneShots = new();
    readonly Stack<AudioSource> freeLoopSources = new();
    readonly List<Voice> loops = new();
    readonly Stack<Voice> loopRecordPool = new();
    readonly Dictionary<SoundEvent, EventState> states = new();
    AudioListener listener;
    int startsThisFrame, frameStamp = -1;
    bool focused = true;

    // debug counters, per second (AudioDebugOverlay)
    internal int Steals { get; private set; }
    internal int Drops { get; private set; }
    int stealCount, dropCount;
    float counterStart;

    internal AudioConfig Config => config;
    internal Voice[] OneShotVoices => oneShots;
    internal List<Voice> LoopVoices => loops;
    public AudioMixer Mixer => config != null ? config.mixer : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        quitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("Audio System");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioSystem>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        config = AudioConfig.LoadOrDefault();
        if (config.mixer != null) config.mixer.updateMode = AudioMixerUpdateMode.UnscaledTime;   // snapshots fade while paused

        listener = gameObject.AddComponent<AudioListener>();

        oneShots = new Voice[config.oneShotVoices];
        for (int i = 0; i < oneShots.Length; i++)
        {
            oneShots[i] = new Voice { source = MakeSource("Voice " + i) };
            freeOneShots.Push(oneShots[i]);
        }
        for (int i = 0; i < config.loopVoices; i++) freeLoopSources.Push(MakeSource("Loop " + i));

        // the directors: world-wide listeners with no prefab of their own to live on
        gameObject.AddComponent<ItemAudioDirector>();
        gameObject.AddComponent<BuildAudio>();
        gameObject.AddComponent<MenuAudio>();
        gameObject.AddComponent<AmbienceDirector>();
        gameObject.AddComponent<MusicDirector>();
        gameObject.AddComponent<MixDirector>();

        if (Debug.isDebugBuild) gameObject.AddComponent<AudioDebugOverlay>();

        GameStateManager.StateChanged += OnGameStateChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        Application.quitting += OnQuitting;
    }

    // Mixer values set in Awake are reset before Start (Unity, "by design"), so the
    // saved volumes go on here.
    void Start() => AudioSettingsStore.ApplyAll();

    void OnDestroy()
    {
        if (Instance != this) return;
        GameStateManager.StateChanged -= OnGameStateChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Application.quitting -= OnQuitting;
        Instance = null;
    }

    static void OnQuitting() => quitting = true;

    AudioSource MakeSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.dopplerLevel = 0f;
        return s;
    }

    // ---------------------------------------------------------------- one-shots

    internal SoundHandle PlayOneShot(SoundEvent e, Transform follow, Vector2 point, bool forceScreen,
                                     float volume, float pitch, int depth = 0)
    {
        if (e == null || quitting) return default;

        float now = Time.unscaledTime;
        EventState st = State(e);
        if (now - st.lastStart < e.cooldown) return default;

        // layers ride on this event's cooldown, then keep their own limits
        if (depth < 3 && e.layers != null)
            foreach (SoundEvent layer in e.layers)
                if (layer != null && layer != e) PlayOneShot(layer, follow, point, forceScreen, volume, pitch, depth + 1);

        if (e.clips == null || e.clips.Length == 0)
        {
            st.lastStart = now;   // a pure container of layers
            return default;
        }

        if (Time.frameCount != frameStamp) { frameStamp = Time.frameCount; startsThisFrame = 0; }
        bool critical = e.priority == SoundEvent.SoundPriority.Critical;
        if (startsThisFrame >= config.maxStartsPerFrame && !critical) { dropCount++; return default; }

        bool screen = forceScreen || e.space == SoundEvent.SoundSpace.Screen;
        Vector2 pos = follow != null ? (Vector2)follow.position : point;
        float att = screen ? 1f : AudioSpace.Gain(config, AudioSpace.RadiiAway(config, pos, e.rangeScale));
        float baseGain = DbToLinear(e.volumeDb + Random.Range(-e.volumeJitterDb, e.volumeJitterDb)) * Mathf.Max(0f, volume);
        if (!critical && att * baseGain < DbToLinear(config.cullDb)) return default;

        // the event's own cap
        if (CountOneShots(st) >= e.maxVoices)
        {
            if (e.steal == SoundEvent.StealMode.None) { dropCount++; return default; }
            Voice own = PickOwnVictim(st, e.steal);
            if (own != null) Release(own);
        }

        Voice v = freeOneShots.Count > 0 ? freeOneShots.Pop() : StealOneShot(e.priority, att * baseGain);
        if (v == null) { dropCount++; return default; }

        AudioClip clip = Pick(e, st);
        if (clip == null) { freeOneShots.Push(v); return default; }

        float p = Mathf.Max(0.05f, pitch) * Semitones(Random.Range(-e.pitchJitterSemitones, e.pitchJitterSemitones));
        float delay = e.startDelay + (e.startDelayJitter > 0f ? Random.Range(0f, e.startDelayJitter) : 0f);

        Begin(v, e, clip, follow, point, screen, baseGain, p, now);
        v.remaining = delay + clip.length / p;
        v.gain = att * baseGain;

        AudioSource s = v.source;
        s.loop = false;
        s.volume = v.gain;
        s.panStereo = screen ? 0f : AudioSpace.Pan(config, pos);
        if (delay > 0f) s.PlayDelayed(delay); else s.Play();

        st.lastStart = now;
        st.live.Add(v);
        startsThisFrame++;
        return new SoundHandle(v);
    }

    static int CountOneShots(EventState st)
    {
        int n = 0;
        foreach (Voice v in st.live) if (!v.loop) n++;
        return n;
    }

    static Voice PickOwnVictim(EventState st, SoundEvent.StealMode mode)
    {
        Voice pick = null;
        foreach (Voice v in st.live)
        {
            if (v.loop) continue;
            if (pick == null
                || (mode == SoundEvent.StealMode.Oldest && v.started < pick.started)
                || (mode == SoundEvent.StealMode.Quietest && v.gain < pick.gain))
                pick = v;
        }
        return pick;
    }

    // The pool is full: take the voice that matters least, if it matters less than the new one.
    Voice StealOneShot(SoundEvent.SoundPriority priority, float gain)
    {
        Voice pick = null;
        foreach (Voice v in oneShots)
        {
            if (!v.active) continue;
            bool lower = v.ev.priority < priority || (v.ev.priority == priority && v.gain < gain);
            if (!lower) continue;
            if (pick == null || v.ev.priority < pick.ev.priority
                || (v.ev.priority == pick.ev.priority && (v.gain < pick.gain || (v.gain == pick.gain && v.started < pick.started))))
                pick = v;
        }
        if (pick == null) return null;
        Release(pick);
        stealCount++;
        return freeOneShots.Pop();
    }

    // ---------------------------------------------------------------- loops

    internal SoundHandle PlayLoop(SoundEvent e, Transform follow, Vector2 point, bool forceScreen,
                                  float volume, float playhead)
    {
        if (e == null || quitting) return default;
        EventState st = State(e);
        AudioClip clip = Pick(e, st);
        if (clip == null) return default;

        Voice v = loopRecordPool.Count > 0 ? loopRecordPool.Pop() : new Voice();
        float p = Semitones(Random.Range(-e.pitchJitterSemitones, e.pitchJitterSemitones));
        Begin(v, e, clip, follow, point, forceScreen || e.space == SoundEvent.SoundSpace.Screen,
              DbToLinear(e.volumeDb), p, Time.unscaledTime);
        v.loop = true;
        v.source = null;
        v.SetUserGain(volume, 0f);
        // a random point in the clip, so several fires don't play in phase; a layered
        // loop (the campfire's stages) passes one shared playhead to stay in sync
        v.playhead = playhead >= 0f ? playhead % clip.length : Random.Range(0f, clip.length);

        loops.Add(v);
        st.live.Add(v);
        return new SoundHandle(v);
    }

    bool AcquireLoopSource(Voice v)
    {
        AudioSource s = null;
        if (freeLoopSources.Count > 0) s = freeLoopSources.Pop();
        else
        {
            // steal from the audible loop that matters least, if it matters less
            Voice pick = null;
            foreach (Voice o in loops)
            {
                if (o.source == null || o == v) continue;
                bool lower = o.ev.priority < v.ev.priority || (o.ev.priority == v.ev.priority && o.gain < v.gain);
                if (lower && (pick == null || o.ev.priority < pick.ev.priority || o.gain < pick.gain)) pick = o;
            }
            if (pick == null) return false;
            ReleaseLoopSource(pick);
            stealCount++;
            s = freeLoopSources.Pop();
        }

        v.source = s;
        s.clip = v.clip;
        s.loop = true;
        s.outputAudioMixerGroup = v.ev.output;
        s.ignoreListenerPause = v.ev.ignorePause;
        s.priority = SourcePriority(v.ev.priority);
        s.time = Mathf.Clamp(v.playhead, 0f, Mathf.Max(0f, v.clip.length - 0.01f));
        s.Play();
        return true;
    }

    void ReleaseLoopSource(Voice v)
    {
        if (v.source == null) return;
        if (v.source.clip != null) v.playhead = v.source.time;
        v.source.Stop();
        v.source.clip = null;
        freeLoopSources.Push(v.source);
        v.source = null;
    }

    // ---------------------------------------------------------------- shared

    void Begin(Voice v, SoundEvent e, AudioClip clip, Transform follow, Vector2 point, bool screen,
               float baseGain, float pitch, float now)
    {
        v.active = true;
        v.loop = false;
        v.stopping = false;
        v.ev = e;
        v.clip = clip;
        v.follow = follow;
        v.hasFollow = follow != null;
        v.point = follow != null ? (Vector2)follow.position : point;
        v.screen = screen;
        v.baseGain = baseGain;
        v.basePitch = pitch;
        v.pitchScale = 1f;
        v.userGain = v.userGainTarget = 1f;
        v.userGainSpeed = 0f;
        v.fade = 1f;
        v.fadeSpeed = 0f;
        v.started = now;

        if (v.source != null)
        {
            AudioSource s = v.source;
            s.clip = clip;
            s.outputAudioMixerGroup = e.output;
            s.ignoreListenerPause = e.ignorePause;
            s.priority = SourcePriority(e.priority);
            s.pitch = pitch;
        }
    }

    void Release(Voice v)
    {
        if (!v.active) return;
        v.active = false;
        v.generation++;
        if (states.TryGetValue(v.ev, out EventState st)) st.live.Remove(v);
        v.follow = null;

        if (v.loop)
        {
            ReleaseLoopSource(v);
            loops.Remove(v);
            loopRecordPool.Push(v);
        }
        else
        {
            v.source.Stop();
            v.source.clip = null;
            freeOneShots.Push(v);
        }
        v.ev = null;
        v.clip = null;
    }

    EventState State(SoundEvent e)
    {
        if (!states.TryGetValue(e, out EventState st)) states[e] = st = new EventState();
        return st;
    }

    static AudioClip Pick(SoundEvent e, EventState st)
    {
        AudioClip[] clips = e.clips;
        int n = clips != null ? clips.Length : 0;
        if (n == 0) return null;

        int i;
        switch (e.selection)
        {
            case SoundEvent.Selection.Sequential: i = st.sequence++ % n; break;
            case SoundEvent.Selection.Random: i = Random.Range(0, n); break;
            default:
                if (n == 1) { i = 0; break; }
                if (st.bag == null || st.bag.Length != n)
                {
                    st.bag = new int[n];
                    for (int k = 0; k < n; k++) st.bag[k] = k;
                    st.bagIndex = n;
                }
                if (st.bagIndex >= n)
                {
                    for (int k = n - 1; k > 0; k--)
                    {
                        int j = Random.Range(0, k + 1);
                        (st.bag[k], st.bag[j]) = (st.bag[j], st.bag[k]);
                    }
                    if (st.bag[0] == st.last) (st.bag[0], st.bag[n - 1]) = (st.bag[n - 1], st.bag[0]);
                    st.bagIndex = 0;
                }
                i = st.bag[st.bagIndex++];
                break;
        }
        st.last = i;
        return clips[i];
    }

    static int SourcePriority(SoundEvent.SoundPriority p) => p switch
    {
        SoundEvent.SoundPriority.Critical => 0,
        SoundEvent.SoundPriority.High => 64,
        SoundEvent.SoundPriority.Normal => 128,
        _ => 200,
    };

    public static float DbToLinear(float db) => db <= -80f ? 0f : Mathf.Pow(10f, db / 20f);
    static float Semitones(float st) => Mathf.Pow(2f, st / 12f);

    // ---------------------------------------------------------------- per frame

    void LateUpdate()
    {
        AudioSpace.Refresh();
        float dt = Time.unscaledDeltaTime;
        bool listenerPaused = AudioListener.pause;

        foreach (Voice v in oneShots)
        {
            if (!v.active) continue;
            bool paused = listenerPaused && !v.ev.ignorePause;
            if (!paused)
            {
                v.remaining -= dt;
                Step(v, dt);
            }
            if (v.remaining <= 0f || (v.stopping && v.fade <= 0f)) { Release(v); continue; }
            Apply(v, v.source);
        }

        for (int i = loops.Count - 1; i >= 0; i--)
        {
            Voice v = loops[i];
            bool paused = listenerPaused && !v.ev.ignorePause;
            if (!paused) Step(v, dt);
            if (v.stopping && v.fade <= 0f) { Release(v); continue; }
            // whoever owned the loop is gone without stopping it
            if (v.hasFollow && v.follow == null) { v.hasFollow = false; v.BeginStop(0.2f); }

            Vector2 pos = Position(v);
            float radii = v.screen ? 0f : AudioSpace.RadiiAway(config, pos, v.ev.rangeScale);
            bool wanted = v.userGain * v.fade > 0.0005f || v.userGainTarget > 0f;
            if (v.source == null)
            {
                if (wanted && radii < 1f) AcquireLoopSource(v);
            }
            else if (radii > config.loopReleaseRadii || !wanted) ReleaseLoopSource(v);

            if (v.source != null)
            {
                Apply(v, v.source);
                v.playhead = v.source.time;
            }
            else
            {
                v.gain = 0f;
                if (!paused && v.clip != null) v.playhead = (v.playhead + dt * v.basePitch * v.pitchScale) % v.clip.length;
            }
        }

        if (Time.unscaledTime - counterStart >= 1f)
        {
            Steals = stealCount; Drops = dropCount;
            stealCount = dropCount = 0;
            counterStart = Time.unscaledTime;
        }
    }

    static void Step(Voice v, float dt)
    {
        if (v.userGainSpeed > 0f) v.userGain = Mathf.MoveTowards(v.userGain, v.userGainTarget, v.userGainSpeed * dt);
        if (v.stopping) v.fade = Mathf.MoveTowards(v.fade, 0f, v.fadeSpeed * dt);
    }

    static Vector2 Position(Voice v)
    {
        if (v.hasFollow)
        {
            if (v.follow != null) v.point = v.follow.position;
            else v.hasFollow = false;   // keep sounding where it was
        }
        return v.point;
    }

    void Apply(Voice v, AudioSource s)
    {
        Vector2 pos = Position(v);
        float att = v.screen ? 1f : AudioSpace.Gain(config, AudioSpace.RadiiAway(config, pos, v.ev.rangeScale));
        v.gain = att * v.baseGain * v.userGain * v.fade;
        s.volume = Mathf.Clamp01(v.gain);
        s.panStereo = v.screen ? 0f : AudioSpace.Pan(config, pos);
        s.pitch = v.basePitch * v.pitchScale;
    }

    // ---------------------------------------------------------------- state

    void OnGameStateChanged(GameState state)
    {
        AudioListener.pause = state == GameState.Paused;

        AudioMixer mixer = Mixer;
        if (mixer == null) return;
        string name = state switch
        {
            GameState.Playing => config.gameplaySnapshot,
            GameState.Paused => config.pausedSnapshot,
            GameState.MainMenu => config.menuSnapshot,
            _ => config.endSnapshot,
        };
        AudioMixerSnapshot snapshot = mixer.FindSnapshot(name);
        if (snapshot != null)
            snapshot.TransitionTo(state is GameState.GameOver or GameState.GameWon ? config.endFade : config.snapshotFade);
    }

    // Builds only: in the editor the Game view loses focus whenever you click the
    // inspector, which would mute every preview.
    void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (Application.isEditor) return;
        AudioListener.volume = !focused && AudioSettingsStore.MuteWhenUnfocused ? 0f : 1f;
    }

    // The 2D model needs exactly one listener: this one. A camera that brought its own
    // (Unity adds one to every new camera) is switched off.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (AudioListener other in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (other == listener || !other.enabled) continue;
            other.enabled = false;
            Debug.Log($"AudioSystem: disabled the AudioListener on '{other.name}' (the audio layer owns the listener).", other);
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        foreach (Voice v in oneShots) if (v.active && !v.screen) Release(v);
        for (int i = loops.Count - 1; i >= 0; i--) if (!loops[i].screen) Release(loops[i]);
    }

    // ---------------------------------------------------------------- handles

    internal void Stop(Voice v) => Release(v);
}
