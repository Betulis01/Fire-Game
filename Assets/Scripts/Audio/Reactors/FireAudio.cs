using UnityEngine;

// A fire, heard from its Fuel (sound.md §8.1, §8.2, §4.3) -- the campfire, the furnace,
// a torch -- through its FireSoundSet. Every frame the layers follow the fuel level
// (equal-power crossfades, in sync from one playhead, so the fire swells and dies
// smoothly), and the edges play their one-shots: a stage up, the low sputter, going out,
// relighting, and ember pops more often the bigger it burns. Fuel going in plays its
// feed, by what it was (Fuel.FuelAdded's item).
//
// The loops live only while this is enabled: a torch stowed onto the back or put in
// the bag is inactive, so it falls silent with its flame; a placement ghost's fire stays
// off (PlacementGhost disables it) until it's built.
[RequireComponent(typeof(Fuel))]
public class FireAudio : MonoBehaviour
{
    public FireSoundSet sounds;

    Fuel fuel;
    SoundHandle[] layers = new SoundHandle[0];
    bool wasOut, lowArmed;
    int stage;
    float nextPop, speed;
    Vector2 lastPos;

    void Awake() => fuel = GetComponent<Fuel>();

    void OnEnable()
    {
        fuel.FuelAdded += OnFuelAdded;
        if (sounds == null) return;

        float f = fuel.fuelLevel;
        wasOut = f <= 0f;
        stage = Stage(f);
        lowArmed = f > sounds.lowAt;
        lastPos = transform.position;
        speed = 0f;
        SchedulePop(f);

        // one shared playhead keeps the layers in step (same-length loops)
        float head = Random.Range(0f, 600f);
        layers = new SoundHandle[sounds.layers.Length];
        for (int i = 0; i < layers.Length; i++)
            layers[i] = Sfx.PlayLoop(sounds.layers[i].loop, transform, 0f, head);
        ApplyLayers(f, 0f);
    }

    void OnDisable()
    {
        fuel.FuelAdded -= OnFuelAdded;
        foreach (SoundHandle h in layers) h.Stop(0.15f);
        layers = new SoundHandle[0];
    }

    void Update()
    {
        if (sounds == null) return;
        float f = fuel.fuelLevel;
        bool isOut = f <= 0f;

        if (isOut != wasOut)
        {
            wasOut = isOut;
            Sfx.Play(isOut ? sounds.outSound : sounds.relight, transform.position);
        }

        int s = Stage(f);
        if (s > stage && !isOut) Sfx.Play(sounds.stageUp, transform.position);
        stage = s;

        if (lowArmed && !isOut && f <= sounds.lowAt) { lowArmed = false; Sfx.Play(sounds.low, transform.position); }
        else if (!lowArmed && f > sounds.lowAt + 0.05f) lowArmed = true;

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            Vector2 pos = transform.position;
            speed = Mathf.Lerp(speed, (pos - lastPos).magnitude / dt, 1f - Mathf.Exp(-dt * 6f));
            lastPos = pos;
        }
        ApplyLayers(f, 0.25f);

        if (!isOut && Time.time >= nextPop)
        {
            Sfx.Play(sounds.pop, transform.position);
            SchedulePop(f);
        }
    }

    int Stage(float f) => Mathf.Clamp(Mathf.FloorToInt(f * sounds.stageCount), 0, sounds.stageCount - 1);

    void SchedulePop(float f) =>
        nextPop = Time.time + Mathf.Lerp(sounds.popInterval.x, sounds.popInterval.y, f) * Random.Range(0.6f, 1.4f);

    // Each layer's volume for fuel f: full at its centre, an equal-power crossfade to its
    // neighbour in between, the lowest one below it and the highest above; silent when out.
    void ApplyLayers(float f, float fade)
    {
        int n = layers.Length;
        if (n == 0) return;
        float boost = sounds.motionBoost > 0f ? Mathf.Lerp(1f - sounds.motionBoost, 1f, Mathf.Clamp01(speed / 3f)) : 1f;

        for (int i = 0; i < n; i++)
        {
            float w;
            if (f <= 0f) w = 0f;
            else if (n == 1) w = 1f;
            else
            {
                float c = sounds.layers[i].centre;
                float prev = i > 0 ? sounds.layers[i - 1].centre : float.NegativeInfinity;
                float next = i < n - 1 ? sounds.layers[i + 1].centre : float.PositiveInfinity;
                if (f <= c) w = i == 0 ? 1f : f <= prev ? 0f : Mathf.Sin((f - prev) / (c - prev) * Mathf.PI * 0.5f);
                else w = i == n - 1 ? 1f : f >= next ? 0f : Mathf.Cos((f - c) / (next - c) * Mathf.PI * 0.5f);
            }
            layers[i].SetVolume(w * boost, fade);
        }
    }

    void OnFuelAdded(float amount, ItemDefinition item)
    {
        if (sounds == null) return;
        FuelType? type = null;
        if (item != null && item.prefab != null && item.prefab.TryGetComponent(out Burnable burnable)) type = burnable.fuelType;
        Sfx.Play(sounds.FeedFor(type), transform.position);
    }
}
