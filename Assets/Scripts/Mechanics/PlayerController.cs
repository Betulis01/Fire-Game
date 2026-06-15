using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Hands), typeof(BodyTemperature))]
public class PlayerController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    BodyTemperature body;

    public float speed = 5f;

    [Header("Temperature affects speed")]
    public float coldTemp = -1f;            // felt <= this -> slowest
    public float warmTemp = 0f;              // felt >= this -> full speed
    [Range(0f, 1f)] public float speedFloor = 0.4f;   // slowest fraction of base speed
    public float smoothTime = 0.5f;          // how quickly speed eases to the target

    float speedMultiplier = 1f;
    float smoothVel;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<BodyTemperature>();
    }

    void Update()
    {
        // cold drags speed toward the floor; warmth eases it back to full
        float t = Mathf.InverseLerp(coldTemp, warmTemp, body.Felt);   // 0 cold .. 1 warm
        float target = Mathf.Lerp(speedFloor, 1f, t);
        speedMultiplier = Mathf.SmoothDamp(speedMultiplier, target, ref smoothVel, smoothTime);

        float x = Input.GetAxisRaw("Horizontal"); // A/D or arrows
        float y = Input.GetAxisRaw("Vertical");   // W/S or arrows

        Vector3 move = new Vector3(x, y, 0f).normalized;
        transform.Translate(move * speed * speedMultiplier * Time.deltaTime);
    }
}
