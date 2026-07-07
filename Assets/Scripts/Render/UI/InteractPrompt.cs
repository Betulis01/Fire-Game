using TMPro;
using UnityEngine;

// A single reusable world-space prompt that PlayerInteractor moves over
// whichever interactable is currently in focus. Attach to a prefab that has
// both a SpriteRenderer (the "E" icon) and a TMP_Text child for optional text.
public class InteractPrompt : MonoBehaviour
{
    [Tooltip("Optional TMP_Text child for dynamic labels (e.g. 'Drop Stone (0/1)').")]
    public TMP_Text label;

    SpriteRenderer sr;
    public TMP_Text pressString;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        Hide();
    }

    public void Show(Vector3 worldPos)
    {
        transform.position = worldPos;
        if (!sr.enabled) sr.enabled = true;
        if (label != null) label.enabled = true;
        if (pressString != null) pressString.enabled = true;
    }

    public void Show(Vector3 worldPos, string text)
    {
        if (label != null)
        {
            label.text = text;
            label.enabled = true;
        }
        Show(worldPos);
    }

    public void Hide()
    {
        if (sr.enabled) sr.enabled = false;
        if (label != null && label.enabled) label.enabled = false;
        if (pressString != null && pressString.enabled) pressString.enabled = false;
    }
}
