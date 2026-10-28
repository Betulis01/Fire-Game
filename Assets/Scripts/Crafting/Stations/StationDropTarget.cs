using UnityEngine;
using UnityEngine.EventSystems;

// On a station's world-space menu (StationMenuFrame): a bag slot dragged onto the open
// menu is fed to the station's ItemReceivers -- burnables into its Fuel, ore or food
// into its processor -- one item at a time until they stop taking it.
public class StationDropTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        StationInteractable station = GetComponentInParent<StationInteractable>();
        if (station == null) return;
        if (BagDrag.Claim(out Inventory inv, out int slot)) inv.DepositInto(slot, station.gameObject);
    }
}
