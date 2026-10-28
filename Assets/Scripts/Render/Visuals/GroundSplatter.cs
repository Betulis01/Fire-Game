using System.Collections.Generic;
using UnityEngine;

// A stain lying flat on the ground (a blood splatter, ...): on spawn it picks one of
// its sprite variations, a random horizontal flip and a slight size change, so a
// handful of drawn splats never repeat visibly. It stays for good; the oldest ones
// are cleared once more than maxAlive exist, so a long fight can't pile up renderers
// without end. Sorts on the prefab's SpriteRenderer (Background, above the ground
// tilemap and the snow trail, below SpriteShadow's shadows).
[RequireComponent(typeof(SpriteRenderer))]
public class GroundSplatter : MonoBehaviour
{
    [SerializeField] Sprite[] variations;

    [Tooltip("Random size range, as a multiple of the prefab's scale.")]
    [SerializeField] Vector2 scaleRange = new(0.8f, 1.2f);

    [Tooltip("Most splatters in the world at once (all kinds together); the oldest go first.")]
    [Min(1)] [SerializeField] int maxAlive = 150;

    static readonly Queue<GroundSplatter> alive = new();

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (variations != null && variations.Length > 0)
            sr.sprite = variations[Random.Range(0, variations.Length)];
        sr.flipX = Random.value > 0.5f;
        transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);

        alive.Enqueue(this);
        while (alive.Count > maxAlive)
        {
            GroundSplatter oldest = alive.Dequeue();
            if (oldest != null) Destroy(oldest.gameObject);
        }
    }
}
