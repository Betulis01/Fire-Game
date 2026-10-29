using UnityEngine;

// A carried item in flight after a held-Q throw (PlayerInteractor). It slides along
// the ground from the throw point to the target while a sine arc lifts it, staying
// "held" (pickup collider off) the whole way so nothing grabs it mid-air. While it
// flies it Y-sorts by the ground point it's over (the arc only lifts it), so it
// passes in front of and behind things like anything walking there. On landing
// a receiver whose zone holds the spot and accepts it takes it -- a log thrown into
// the fire becomes fuel, stone onto a build site counts as delivered -- otherwise it
// lies where it fell, marked Dropped like any other drop. Added at runtime and
// removed again on landing.
[RequireComponent(typeof(WorldItem))]
public class ThrownItem : MonoBehaviour
{
    Vector3 from;
    Vector3 to;
    float duration;
    float arcHeight;
    float elapsed;
    YSort ysort;
    float anchorOffset;

    public static void Launch(GameObject item, Vector3 from, Vector3 to, float duration, float arcHeight)
    {
        ThrownItem t = item.AddComponent<ThrownItem>();
        t.from = from;
        t.to = to;
        t.duration = Mathf.Max(0.01f, duration);
        t.arcHeight = arcHeight;
        item.transform.position = from;
        t.ysort = item.GetComponent<YSort>();
        if (t.ysort != null) t.anchorOffset = t.ysort.AnchorOffsetY;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        Vector3 ground = Vector3.Lerp(from, to, t);
        transform.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arcHeight);
        if (ysort != null) ysort.ApplyAt(ground.y + anchorOffset);
        if (t >= 1f) Land();
    }

    void Land()
    {
        transform.position = to;
        WorldItem wi = GetComponent<WorldItem>();

        ItemReceiver receiver = ItemReceiver.FindInZone(to, wi);
        if (receiver != null && receiver.Deposit(wi))
        {
            Destroy(gameObject);   // the receiver took its effect; the item is used up
            return;
        }

        wi.SetHeld(false);
        Destroy(this);
    }
}
