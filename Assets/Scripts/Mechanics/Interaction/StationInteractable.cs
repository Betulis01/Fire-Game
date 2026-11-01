using UnityEngine;

// A fueled processing station (furnace, campfire): E toggles the HUD's station
// panel over it (StationPanelUI), which shows its fuel, the item in progress and the
// finished output. Needs a Fuel and an ItemProcessor on the same object, plus an
// InteractArea child (CircleCollider2D on the Interactable layer).
public class StationInteractable : Interactable
{
    [Tooltip("Where the panel's bottom centre sits, in world units from the station.")]
    public Vector2 menuOffset = new(0f, 1f);

    public bool IsOpen { get; private set; }

    public void Open()
    {
        if (StationPanelUI.Instance == null) return;
        StationPanelUI.Instance.Show(this);
        IsOpen = true;
    }

    public void Close()
    {
        if (StationPanelUI.Instance != null) StationPanelUI.Instance.Hide(this);
        IsOpen = false;
    }

    // another station's panel took over, or this one's went away
    public void Closed() => IsOpen = false;

    public override bool Interact(PlayerController player)
    {
        if (IsOpen) Close();
        else Open();
        return true;
    }
}
