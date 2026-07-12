using UnityEngine;

public class FurnaceInteractable : StationInteractable
{
    [Tooltip("StationProcessPanel prefab instantiated into the menu's content area.")]
    public StationProcessPanel panelPrefab;

    protected override void PopulateMenu(Transform content)
    {
        if (panelPrefab == null) return;
        StationProcessPanel panel = Instantiate(panelPrefab, content, false);
        panel.Bind(GetComponent<Fuel>(), GetComponent<ItemProcessor>());
    }
}
