using UnityEngine;

// A plant the player harvests with E (a Hemp Plant): it hands over one `item` and loses
// its harvestLayers (the Leafs and Snow), leaving the bare stem, until regrowTime has
// passed and they grow back. The item goes where an E pickup would (Inventory.TryTake);
// with no room for it the player fumbles it beside them (PlayerInteractor.Fumble), so a
// harvest never fails. The layers are the art's own renderers, found by name
// (PropArt.Layer); hiding one turns its renderer off, which the importer's clips (they
// key only sprites) never turn back on. A bare stem shows no prompt.
[RequireComponent(typeof(PropArt))]
public class Harvestable : Interactable
{
    [Tooltip("What one harvest gives.")]
    public ItemDefinition item;
    [Tooltip("The art's layers a harvest removes (Aseprite layer names).")]
    [SerializeField] string[] harvestLayers = { "Leafs", "Snow" };
    [Tooltip("Seconds after a harvest before the layers grow back.")]
    public float regrowTime = 60f;

    PropArt art;
    bool grown = true;
    float timer;

    public bool IsGrown => grown;

    void Awake() => art = GetComponent<PropArt>();

    void Update()
    {
        if (grown) return;
        timer += Time.deltaTime;
        if (timer >= regrowTime)
        {
            SetGrown(true);
            Regrown?.Invoke();
        }
    }

    public override bool CanInteract(PlayerController player) => grown && item != null && item.prefab != null;

    public override bool Interact(PlayerController player)
    {
        if (!CanInteract(player)) return false;

        WorldItem yield = Instantiate(item.prefab, transform.position, Quaternion.identity).GetComponent<WorldItem>();
        bool taken = player.TryGetComponent(out Inventory inventory) && inventory.TryTake(yield);
        if (!taken && !(player.TryGetComponent(out PlayerInteractor interactor) && interactor.Fumble(yield)))
        {
            Destroy(yield.gameObject);
            return false;
        }

        SetGrown(false);
        Harvested?.Invoke();
        return true;
    }

    // For the audio layer: picked (the item went to the player or was fumbled), and
    // grown back.
    public event System.Action Harvested;
    public event System.Action Regrown;

    void SetGrown(bool value)
    {
        grown = value;
        timer = 0f;
        foreach (string layerName in harvestLayers)
        {
            SpriteRenderer layer = art.Layer(layerName);
            if (layer != null) layer.enabled = value;
        }
    }
}
