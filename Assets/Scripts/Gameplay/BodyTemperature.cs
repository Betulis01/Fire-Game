using UnityEngine;

// Single source of the temperature the player actually feels: the ambient world
// temperature plus warmth from the nearest heat source (campfire, held torch).
// Both Health and PlayerController read this so the heat-source scan lives once.
public class BodyTemperature : MonoBehaviour
{
    // strongest warmth reaching us right now (0 outside any heat zone).
    // computed on read — cheap for a handful of sources, and order-independent.
    public float Warmth
    {
        get
        {
            float w = 0f;
            foreach (HeatSource s in HeatSource.All)
                w = Mathf.Max(w, s.WarmthAt(transform.position));
            return w;
        }
    }

    // the felt temperature = world ambient lifted by nearby warmth
    public float Felt => Environment.Instance.AmbientTemperature + Warmth;
}
