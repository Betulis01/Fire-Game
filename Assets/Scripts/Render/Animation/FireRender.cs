using UnityEngine;

// Drives a fire's Animator from its Fuel: FuelStage 0..stageCount-1 picks the
// ember/small/medium/roaring (etc.) state. With an outSprite (e.g. the campfire's
// unlit frame), an empty fire stops animating and shows that sprite instead; the
// Animator picks back up as soon as fuel is added. Without one, an empty fire just
// keeps playing its lowest stage.
public class FireRender : MonoBehaviour
{
    public Fuel fuel;
    public Animator animator;

    public int stageCount = 4;   // campfire: ember, small, medium, roaring; torch: ember, medium, roaring

    [Tooltip("Shown while the fuel is empty (the Animator is paused meanwhile). " +
             "Leave empty to keep playing the lowest stage.")]
    public Sprite outSprite;
    [Tooltip("Renderer that shows outSprite. Falls back to one on this GameObject.")]
    public SpriteRenderer spriteRenderer;

    int currentStage = -1;       // track to avoid setting every frame
    bool isOut;

    // Deactivating resets the Animator to its defaults (a torch stowed while carrying,
    // or put in the bag), so resend the stage once it's back.
    void OnEnable() => currentStage = -1;

    void Start()
    {
        if (fuel == null) fuel = GetComponent<Fuel>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        bool outNow = outSprite != null && spriteRenderer != null && fuel.fuelLevel <= 0f;
        if (outNow != isOut)
        {
            isOut = outNow;
            animator.enabled = !outNow;
            if (outNow) spriteRenderer.sprite = outSprite;
            // a re-enabled Animator starts over from its defaults, so resend the stage
            else currentStage = -1;
        }
        if (isOut) return;

        int stage = Mathf.FloorToInt(fuel.fuelLevel * stageCount);
        stage = Mathf.Clamp(stage, 0, stageCount - 1);

        if (stage != currentStage)          // only update when it actually changes
        {
            currentStage = stage;
            animator.SetInteger("FuelStage", stage);
        }
    }
}
