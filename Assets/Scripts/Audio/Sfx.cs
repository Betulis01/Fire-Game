using UnityEngine;

// The one audio API the game calls (SoundSystemPlan.md §3.4). Audio reactors (CreatureAudio,
// FireAudio, ...) and UI handlers play SoundEvents through here and never touch an
// AudioSource. Every call is safe without an AudioSystem (edit mode, a scene being torn
// down, quitting) -- it just does nothing and hands back an empty handle.
//
//   Sfx.Play(e, point)     a one-shot heard from a world point
//   Sfx.Play(e, transform) ... that follows something (an arrow, a creature's voice)
//   Sfx.Play2D(e)          non-positional, whatever the event's space (UI, player status)
//   Sfx.PlayLoop(e, t)     a loop; the caller keeps the handle and Stops it
//
// `volume` and `pitch` scale on top of the event's own level and jitter: a bow release
// louder with more charge, a combo pitching up per step, a build site filling.
public static class Sfx
{
    public static SoundHandle Play(SoundEvent e, Vector2 at, float volume = 1f, float pitch = 1f)
    {
        AudioSystem sys = AudioSystem.Instance;
        return sys != null ? sys.PlayOneShot(e, null, at, false, volume, pitch) : default;
    }

    public static SoundHandle Play(SoundEvent e, Transform follow, float volume = 1f, float pitch = 1f)
    {
        AudioSystem sys = AudioSystem.Instance;
        if (sys == null || follow == null) return default;
        return sys.PlayOneShot(e, follow, follow.position, false, volume, pitch);
    }

    public static SoundHandle Play2D(SoundEvent e, float volume = 1f, float pitch = 1f)
    {
        AudioSystem sys = AudioSystem.Instance;
        return sys != null ? sys.PlayOneShot(e, null, Vector2.zero, true, volume, pitch) : default;
    }

    // `playhead` < 0 starts somewhere random in the clip (so two fires don't play in
    // phase); layers that must stay in sync pass the same value.
    public static SoundHandle PlayLoop(SoundEvent e, Transform follow, float volume = 1f, float playhead = -1f)
    {
        AudioSystem sys = AudioSystem.Instance;
        if (sys == null) return default;
        Vector2 at = follow != null ? (Vector2)follow.position : Vector2.zero;
        return sys.PlayLoop(e, follow, at, follow == null, volume, playhead);
    }

    public static SoundHandle PlayLoopAt(SoundEvent e, Vector2 at, float volume = 1f, float playhead = -1f)
    {
        AudioSystem sys = AudioSystem.Instance;
        return sys != null ? sys.PlayLoop(e, null, at, false, volume, playhead) : default;
    }
}

// A playing sound, by generation: once the voice finishes, is stolen or is stopped, the
// handle goes stale and every call on it does nothing. The default value is stale.
public readonly struct SoundHandle
{
    readonly AudioSystem.Voice voice;
    readonly int generation;

    internal SoundHandle(AudioSystem.Voice v)
    {
        voice = v;
        generation = v.generation;
    }

    public bool IsValid => voice != null && voice.active && voice.generation == generation;

    // A loop that's out of hearing is still valid (it's virtual, see AudioSystem).
    public bool IsAudible => IsValid && voice.source != null;

    // The caller's volume (0..1), eased over `fade` seconds -- e.g. a fire layer
    // following its fuel.
    public void SetVolume(float volume, float fade = 0.1f)
    {
        if (IsValid) voice.SetUserGain(volume, fade);
    }

    public void SetPitch(float pitch)
    {
        if (IsValid) voice.pitchScale = Mathf.Max(0.05f, pitch);
    }

    public void MoveTo(Vector2 point)
    {
        if (!IsValid) return;
        voice.hasFollow = false;
        voice.follow = null;
        voice.point = point;
    }

    public void Stop(float fade = 0.1f)
    {
        if (!IsValid) return;
        if (fade <= 0f && AudioSystem.Instance != null) AudioSystem.Instance.Stop(voice);
        else voice.BeginStop(fade);
    }
}
