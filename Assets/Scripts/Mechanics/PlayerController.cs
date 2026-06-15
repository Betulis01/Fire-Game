using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Inventory))]
public class PlayerController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;

    public float speed = 5f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D or arrows
        float y = Input.GetAxisRaw("Vertical");   // W/S or arrows

        Vector3 move = new Vector3(x, y, 0f).normalized;
        transform.Translate(move * speed * Time.deltaTime);

        if (move.x > 0.01f) {
            spriteRenderer.flipX = false;
        } else if (move.x < -0.01f) {
            spriteRenderer.flipX = true;
        }
    }
}
