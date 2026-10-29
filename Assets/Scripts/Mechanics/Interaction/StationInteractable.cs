using UnityEngine;

// Base for any crafting station that shows a menu over itself (drawn like the HUD)
// when the player presses E. Add a subclass + StationMenuFrame child + InteractArea child
// (CircleCollider2D on the Interactable layer) to a station prefab.
public abstract class StationInteractable : Interactable
{
    [Tooltip("Root of the menu Canvas child; assigned in Inspector.")]
    public GameObject menuRoot;

    [Tooltip("Content area inside the frame; subclasses populate this.")]
    public Transform contentRoot;

    public bool IsOpen { get; private set; }

    bool populated;

    public void Open()
    {
        if (menuRoot == null) return;
        menuRoot.SetActive(true);
        IsOpen = true;
        if (!populated)
        {
            populated = true;
            if (contentRoot != null) PopulateMenu(contentRoot);
        }
    }

    public void Close()
    {
        if (menuRoot == null) return;
        menuRoot.SetActive(false);
        IsOpen = false;
    }

    // the menu canvas leaves the hierarchy on first open (StationMenuFrame), so it
    // isn't destroyed with the station on its own
    void OnDestroy()
    {
        if (menuRoot != null) Destroy(menuRoot);
    }

    // Called once the first time this station's menu is opened.
    // Subclasses instantiate their content into contentRoot here.
    protected abstract void PopulateMenu(Transform content);

    public override bool Interact(PlayerController player, HandSide hand)
    {
        if (IsOpen) Close();
        else Open();
        return true;
    }
}
