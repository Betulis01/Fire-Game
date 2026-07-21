using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One cell in the Blueprint Panel's Required Materials grid: an ingredient's
// icon and the amount needed.
public class MaterialRequirementUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text countLabel;

    public void Set(Sprite sprite, int amount)
    {
        icon.sprite = sprite;
        countLabel.text = amount.ToString();
    }
}
