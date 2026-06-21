using System.Collections.Generic;
using UnityEngine;

// Pooled spawner for floating damage numbers. Drop one in the scene (it registers
// itself as the singleton) and assign the DamagePopup prefab. Anything wanting a
// number on screen calls CombatText.Spawn(worldPos, amount, style).
public class CombatText : MonoBehaviour
{
    public static CombatText Instance { get; private set; }

    public DamagePopup popupPrefab;     // world-space TMP prefab with a DamagePopup
    public int prewarm = 16;            // popups pooled up front

    readonly Queue<DamagePopup> pool = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < prewarm; i++) Release(Create());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Spawn(Vector3 worldPos, float amount, PopupStyle style)
    {
        if (Instance == null || Instance.popupPrefab == null) return;
        Instance.SpawnInternal(worldPos, amount, style);
    }

    void SpawnInternal(Vector3 worldPos, float amount, PopupStyle style)
    {
        DamagePopup popup = pool.Count > 0 ? pool.Dequeue() : Create();
        popup.transform.position = worldPos;
        popup.gameObject.SetActive(true);
        popup.Play(amount, style, this);
    }

    DamagePopup Create()
    {
        DamagePopup popup = Instantiate(popupPrefab, transform);
        return popup;
    }

    // Called by a popup when its animation finishes.
    public void Release(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        pool.Enqueue(popup);
    }
}
