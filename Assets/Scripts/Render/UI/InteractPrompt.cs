using TMPro;
using UnityEngine;

// A single reusable world-space "E" label that PlayerInteractor moves over
// whichever interactable is currently in focus. Put this on a prefab whose root
// has a 3D TextMeshPro (TextMeshPro, not the UGUI TextMeshProUGUI).
[RequireComponent(typeof(TMP_Text))]
public class InteractPrompt : MonoBehaviour
{
    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        Hide();
    }

    public void Show(Vector3 worldPos)
    {
        transform.position = worldPos;
        if (!label.enabled) label.enabled = true;
    }

    // overload for prompts with custom, changing text (e.g. "Drop Stone (0/1)")
    public void Show(Vector3 worldPos, string text)
    {
        label.text = text;
        Show(worldPos);
    }

    public void Hide()
    {
        if (label.enabled) label.enabled = false;
    }
}
