using System.Collections.Generic;
using UnityEngine;

// Player-controlled zoom: Page Up / Page Down (UserInput.ZoomIn / ZoomOut) step
// through zoom levels. It only picks the level -- CameraZoom owns the camera's
// orthographic size, eases to the new base size and layers the charged-attack zoom
// on top. The camera's own size is the default: play starts there exactly, and
// it's merged into the levels so stepping can always come back to it.
[RequireComponent(typeof(CameraZoom))]
public class CameraZoomControl : MonoBehaviour
{
    [Tooltip("Extra orthographic sizes to step through around the camera's own size " +
             "(smaller = zoomed in). Order doesn't matter.")]
    [SerializeField] float[] levels = { 3f, 4f, 5f, 6.5f, 8f };

    CameraZoom zoom;
    readonly List<float> steps = new();   // sorted, the camera's size included
    int level;

    // After CameraZoom.Awake has read the camera's size.
    void Start()
    {
        zoom = GetComponent<CameraZoom>();
        float home = zoom.BaseSize;

        steps.Clear();
        steps.Add(home);
        foreach (float size in levels)
            if (size > 0f && !steps.Exists(s => Mathf.Approximately(s, size))) steps.Add(size);
        steps.Sort();
        level = steps.IndexOf(home);
    }

    void Update()
    {
        if (UserInput.Instance == null || steps.Count == 0) return;

        int step = (UserInput.Instance.ZoomOut ? 1 : 0) - (UserInput.Instance.ZoomIn ? 1 : 0);
        if (step == 0) return;

        level = Mathf.Clamp(level + step, 0, steps.Count - 1);
        zoom.BaseSize = steps[level];
    }
}
