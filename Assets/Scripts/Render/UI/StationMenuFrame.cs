using UnityEngine;

// Attached to the world-space Canvas root on each station's menu prefab.
// Finds the main camera at runtime so the Canvas renders and raycasts correctly
// without needing a manual camera reference per prefab.
[RequireComponent(typeof(Canvas))]
public class StationMenuFrame : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
    }
}
