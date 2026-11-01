using UnityEngine;

// Something on fire (sound.md §6.7): the catch when a torch sets it alight, a crackling
// loop on it while it burns, a sizzle per burn tick, and the fizz when it goes out
// (timed out or dead). Beside Flammable on every creature and the target dummy.
[RequireComponent(typeof(Flammable))]
public class FlammableAudio : MonoBehaviour
{
    public SoundEvent ignite;
    public SoundEvent burningLoop;
    public SoundEvent tick;
    public SoundEvent extinguish;

    Flammable flammable;
    SoundHandle loop;

    void Awake() => flammable = GetComponent<Flammable>();

    void OnEnable()
    {
        flammable.Ignited += OnIgnited;
        flammable.Ticked += OnTicked;
        flammable.Extinguished += OnExtinguished;
    }

    void OnDisable()
    {
        flammable.Ignited -= OnIgnited;
        flammable.Ticked -= OnTicked;
        flammable.Extinguished -= OnExtinguished;
        loop.Stop(0.2f);
    }

    void OnIgnited(bool fresh)
    {
        if (!fresh) return;   // a refreshed burn is already crackling
        Sfx.Play(ignite, transform);
        if (!loop.IsValid && burningLoop != null) loop = Sfx.PlayLoop(burningLoop, transform);
    }

    void OnTicked() => Sfx.Play(tick, transform);

    void OnExtinguished()
    {
        loop.Stop(0.3f);
        Sfx.Play(extinguish, transform.position);
    }
}
