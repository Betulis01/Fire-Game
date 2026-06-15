using UnityEngine;

// Draws the player's two hands into two slot widgets (slot 0 = left, 1 = right).
// Redraws only when a hand changes (via Hands.Changed), not per frame.
public class InventoryBar : MonoBehaviour
{
    public Hands hands;
    public InventorySlotUI left;
    public InventorySlotUI right;

    void OnEnable()
    {
        if (hands != null) hands.Changed += Redraw;
    }

    void OnDisable()
    {
        if (hands != null) hands.Changed -= Redraw;
    }

    void Start()
    {
        Redraw();   // draw the initial (empty) state
    }

    void Redraw()
    {
        Draw(left, HandSide.Left);
        Draw(right, HandSide.Right);
    }

    void Draw(InventorySlotUI slot, HandSide side)
    {
        GameObject item = hands.Held(side);
        if (item != null)
        {
            // the world object is its own icon
            slot.Set(item.GetComponent<SpriteRenderer>().sprite, hands.Count(side));
        }
        else
        {
            slot.Clear();
        }
    }
}
