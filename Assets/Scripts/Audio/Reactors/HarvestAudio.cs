using UnityEngine;

// A plant harvested with E, heard (sound.md §9.3): the fibres ripping as it's picked, and
// (quietly, if set) its leaves growing back. Beside Harvestable on the Hemp Plant.
[RequireComponent(typeof(Harvestable))]
public class HarvestAudio : MonoBehaviour
{
    public SoundEvent harvested;
    public SoundEvent regrown;

    Harvestable plant;

    void Awake() => plant = GetComponent<Harvestable>();

    void OnEnable()
    {
        plant.Harvested += OnHarvested;
        plant.Regrown += OnRegrown;
    }

    void OnDisable()
    {
        plant.Harvested -= OnHarvested;
        plant.Regrown -= OnRegrown;
    }

    void OnHarvested() => Sfx.Play(harvested, transform.position);
    void OnRegrown() => Sfx.Play(regrown, transform.position);
}
