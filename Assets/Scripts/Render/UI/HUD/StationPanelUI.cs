using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The HUD layer the open station's panel lives on (mockup 259,682): the fuel box with
// the fuel bar under it, the input box with the progress bar under it, and the output
// box below. A StationInteractable shows it over itself (Show / Hide); the panel sits
// over the station at its menuOffset each frame, on whole reference pixels, so it
// stays crisp and a fixed size at any camera zoom.
// A bag stack dropped on the fuel or input box goes to that receiver only; dropped
// anywhere else on the panel it goes to every receiver that takes it (fuel, then
// input). Clicking the output box takes the finished items (StationSlotUI).
[RequireComponent(typeof(RectTransform))]
[DefaultExecutionOrder(1000)] // after the camera has moved (CameraFollow, CameraShake)
public class StationPanelUI : MonoBehaviour, IDropHandler
{
    public static StationPanelUI Instance { get; private set; }

    [SerializeField] HudRoot root;
    [SerializeField] RectTransform panel;       // the boxes and bars; hidden while closed
    [SerializeField] StationSlotUI fuelSlot;
    [SerializeField] StationSlotUI inputSlot;
    [SerializeField] StationSlotUI outputSlot;
    [SerializeField] Image fuelFill;
    [SerializeField] Image progressFill;

    public StationInteractable Current { get; private set; }

    RectTransform rect;
    Camera cam;
    Fuel fuel;
    ItemProcessor processor;

    void Awake()
    {
        Instance = this;
        rect = (RectTransform)transform;
        panel.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(StationInteractable station)
    {
        if (Current != null && Current != station) Current.Closed();
        Current = station;
        fuel = station.GetComponent<Fuel>();
        processor = station.GetComponent<ItemProcessor>();
        panel.gameObject.SetActive(true);
        LateUpdate();
    }

    public void Hide(StationInteractable station)
    {
        if (station != Current) return;
        Current = null;
        fuel = null;
        processor = null;
        panel.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (Current == null)
        {
            // the station was destroyed while open
            if (panel.gameObject.activeSelf) Hide(Current);
            return;
        }
        Place();

        fuelFill.fillAmount = fuel != null ? fuel.fuelLevel : 0f;
        progressFill.fillAmount = processor != null ? processor.Progress : 0f;

        ItemDefinition burning = fuel != null ? fuel.CurrentItem : null;
        fuelSlot.Set(burning != null ? burning.ResolveIcon() : null, fuel != null ? fuel.ItemCount : 0, PrefabOf(burning));

        ItemDefinition input = processor != null ? processor.CurrentInput : null;
        inputSlot.Set(processor != null ? processor.CurrentIcon : null, processor != null ? processor.QueueCount : 0,
                      PrefabOf(input));

        ItemDefinition output = processor != null ? processor.OutputItem : null;
        outputSlot.Set(output != null ? output.ResolveIcon() : null, processor != null ? processor.OutputCount : 0,
                       PrefabOf(output));
    }

    static GameObject PrefabOf(ItemDefinition item) => item != null ? item.prefab : null;

    // the panel's pivot (bottom centre) over the station's menu point
    void Place()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 screen = cam.WorldToScreenPoint(Current.transform.position + (Vector3)Current.menuOffset);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out Vector2 local))
            panel.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
    }

    // A bag stack dropped on the panel outside the fuel and input boxes.
    public void OnDrop(PointerEventData eventData) => Drop(StationSlotUI.Kind.Output);

    public void Drop(StationSlotUI.Kind box)
    {
        if (Current == null || !BagDrag.Claim(out Inventory inv, out int slot)) return;
        switch (box)
        {
            case StationSlotUI.Kind.Fuel:
                if (Current.TryGetComponent(out FuelReceiver fuelIn)) inv.Deposit(slot, fuelIn);
                break;
            case StationSlotUI.Kind.Input:
                if (Current.TryGetComponent(out ProcessorInputReceiver input)) inv.Deposit(slot, input);
                break;
            default:
                inv.DepositInto(slot, Current.gameObject);
                break;
        }
    }

    public void TakeOutput()
    {
        if (processor != null && root != null) processor.TakeOutput(root.Inventory);
    }
}
