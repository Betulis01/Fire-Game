using UnityEngine;
[ExecuteAlways]
public class RandomSprite : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;

    void Awake()
    {
        if (sprites.Length == 0) return;

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = sprites[Random.Range(0, sprites.Length)];
    }
}