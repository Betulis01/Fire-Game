using UnityEngine;

public class CampfireRender : MonoBehaviour
{
    public Fuel fuel;
    public Animator animator;

    public int stageCount = 4;   // ember, small, medium, roaring

    int currentStage = -1;       // track to avoid setting every frame

    void Start()
    {
        if (fuel == null) fuel = GetComponent<Fuel>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        int stage = Mathf.FloorToInt(fuel.fuelLevel * stageCount);
        stage = Mathf.Clamp(stage, 0, stageCount - 1);

        if (stage != currentStage)          // only update when it actually changes
        {
            currentStage = stage;
            animator.SetInteger("FuelStage", stage);
        }
    }
}