using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reusable station menu face: a square input slot in the middle with two
// rings around it — inner ring is the current process progress, outer ring is
// the fuel level. Pure display: Bind() it to a station's Fuel and
// ItemProcessor and it polls them every frame. Which process runs behind it
// (smelting, cooking) is the station's business.
public class StationProcessPanel : MonoBehaviour
{
    public RadialGauge fuelRing;
    public RadialGauge processRing;
    public Image inputIcon;
    public TMP_Text queueCount;

    [Tooltip("Room for the item icon, in reference pixels: the input field's inside " +
             "(21 art pixels) at the panel's x3. Icons draw at a whole-number scale in it.")]
    public Vector2 iconBox = new(63f, 63f);

    Fuel fuel;
    ItemProcessor processor;
    Sprite shownIcon;

    public void Bind(Fuel fuel, ItemProcessor processor)
    {
        this.fuel = fuel;
        this.processor = processor;
    }

    void Update()
    {
        if (fuelRing != null) fuelRing.Set(fuel != null ? fuel.fuelLevel : 0f);
        if (processRing != null) processRing.Set(processor != null ? processor.Progress : 0f);

        Sprite icon = processor != null ? processor.CurrentIcon : null;
        if (inputIcon != null && (icon != shownIcon || inputIcon.enabled != (icon != null)))
        {
            shownIcon = icon;
            UiPixel.FitIntegerScale(inputIcon, icon, iconBox);
        }

        if (queueCount != null)
        {
            int n = processor != null ? processor.QueueCount : 0;
            queueCount.text = n > 1 ? $"x{n}" : "";
        }
    }
}
