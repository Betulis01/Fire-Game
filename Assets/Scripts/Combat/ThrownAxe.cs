using System;
using UnityEngine;

// The axe in flight from AxeThrowSkill, spawned from the Thrown Axe prefab
// (Prefabs/Skills): this root moves and Y-sorts; its `spin` child turns, carrying
// `art`, which takes on the thrown axe's own sprite, flip and material and sits
// offset so it spins about its middle, not the grip it's pivoted on; `shadow` lies on
// the ground under it (a small ellipse is made when it has no sprite of its own).
// Add trails or particles to the prefab freely: under `spin` they turn with the axe,
// under the root they only follow it. It flies at the height of the hand it left
// (Y-sorting by the ground point it's over, like ThrownItem) and comes back the same
// way:
//   Out   -- a quadratic Bezier to its one target's hurt collider, swung out to the
//            side opposite the return (so out and back trace a loop), its end
//            re-aimed every frame so a moving target still gets hit; it passes
//            through everything else. On contact it lands the hit (HitResolution, so
//            knockback, effects and XP all apply) and sticks.
//            No target (it curves to `range` along the aim), a target that died or
//            got further than 1.5x `range`: it loses speed over the last stretch and
//            turns back.
//   Stuck -- hangs in the target for `stickTime`, riding along if it moves, not spinning.
//   Back  -- a quadratic Bezier to the hand, its end tracking the hand live and its
//            control point swung out to one side, for the sweeping return. Slow to
//            start, then a whoosh. Over the last quarter the spin settles into the
//            held axe's own pose, so the hand-off doesn't pop.
// Arriving calls `caught` and the axe is gone (the skill shows the held one again).
public class ThrownAxe : MonoBehaviour
{
    [Serializable]
    public struct Settings
    {
        [Tooltip("Furthest it flies with no target (it chases one up to 1.5x this).")]
        public float range;
        public float outSpeed;
        [Tooltip("Top speed on the way back (it eases in from a third of this).")]
        public float returnSpeed;
        [Tooltip("Seconds it hangs in what it hit before flying back.")]
        public float stickTime;
        [Tooltip("Degrees per second.")]
        public float spinSpeed;
        [Tooltip("How far the return swings out to the side, as a fraction of its length.")]
        public float returnCurve;
    }

    enum Phase { Out, Stuck, Back }

    const float HitRadius = 0.12f;      // this close to the hurt collider counts as contact
    const float SlowdownFraction = 0.3f; // the last stretch of a miss loses speed
    const float SettleFraction = 0.25f;  // the last stretch home settles into the held pose

    [SerializeField] Transform spin;
    [SerializeField] SpriteRenderer art;
    [SerializeField] SpriteRenderer shadow;

    Settings cfg;
    GameObject owner;
    AttackData attack;
    Hurtbox target;
    Transform hand;
    SpriteRenderer held;
    float curveSide;
    Action caught;

    YSort ysort;
    float height;
    Phase phase;
    Vector2 dir;
    Vector2 outFrom;
    Vector2 outTo;       // the Out curve's end: the target's latest contact point
    float outT;
    bool lost;           // the target died or got away: the curve's end stays put
    float stuckUntil;
    Vector2 stuckOffset;
    Vector2 backFrom;
    float backT;
    float angle;
    float spinSign;

    static Sprite shadowSprite;

    // Throw a `prefab` from `hand` (whose `held` renderer is the axe being thrown) along
    // `aim`, at `target` (null: straight along the aim). `height` is the hand's height
    // above the ground; `curveSide` (+1/-1) which side the return swings out to.
    public static ThrownAxe Launch(ThrownAxe prefab, SpriteRenderer held, Transform hand, float height, Vector2 aim,
                                   Hurtbox target, GameObject owner, in AttackData attack, in Settings cfg,
                                   float curveSide, Action caught)
    {
        ThrownAxe axe = Instantiate(prefab, CenterOf(held), Quaternion.identity);
        axe.name = prefab.name;
        axe.spin.SetPositionAndRotation(axe.transform.position, held.transform.rotation);
        axe.spin.localScale = held.transform.lossyScale;
        axe.art.sprite = held.sprite;
        axe.art.sharedMaterial = held.sharedMaterial;
        axe.art.sortingLayerID = held.sortingLayerID;
        axe.SetFlip(held.flipX);
        axe.ysort = axe.GetComponent<YSort>();
        if (axe.shadow != null)
        {
            if (axe.shadow.sprite == null) axe.shadow.sprite = DefaultShadow();
            axe.shadow.sortingLayerID = held.sortingLayerID;
        }

        axe.cfg = cfg;
        axe.owner = owner;
        axe.attack = attack;
        axe.target = target;
        axe.hand = hand;
        axe.held = held;
        axe.height = height;
        axe.curveSide = curveSide;
        axe.caught = caught;
        axe.dir = aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector2.right;
        axe.angle = held.transform.eulerAngles.z;
        axe.spinSign = axe.dir.x >= 0f ? -1f : 1f;   // over the top, toward the throw
        axe.outFrom = axe.transform.position;
        axe.outTo = Valid(target) ? target.HurtCollider.ClosestPoint(axe.outFrom) : axe.outFrom + axe.dir * cfg.range;
        axe.Place(axe.transform.position);
        return axe;
    }

    void Update()
    {
        float dt = Time.deltaTime;   // hit stop freezes it with everything else
        if (dt <= 0f) return;

        switch (phase)
        {
            case Phase.Out: FlyOut(dt); break;
            case Phase.Stuck: Stick(); break;
            case Phase.Back: FlyBack(dt); break;
        }
    }

    void FlyOut(float dt)
    {
        if (!lost && target != null)
        {
            if (!Valid(target)) lost = true;
            else
            {
                Vector2 at = target.HurtCollider.ClosestPoint(transform.position);
                if (Vector2.Distance(outFrom, at) > cfg.range * 1.5f) lost = true;
                else outTo = at;
            }
        }
        bool chasing = target != null && !lost;

        // the Out curve swings to the side opposite the return's (Control on the
        // reversed chord), a miss loses speed over its last stretch
        Vector2 control = Control(outFrom, outTo);
        float slow = chasing ? 1f : Mathf.Lerp(0.3f, 1f, Mathf.Clamp01((1f - outT) / SlowdownFraction));
        outT = Mathf.Min(1f, outT + cfg.outSpeed * slow * dt / Length(outFrom, control, outTo));
        Vector2 pos = Bezier(outFrom, control, outTo, outT);
        Spin(dt);
        Place(pos);

        if (chasing)
        {
            Vector2 contact = target.HurtCollider.ClosestPoint(pos);
            if (outT >= 1f || Vector2.Distance(pos, contact) <= HitRadius) Hit(contact);
        }
        else if (outT >= 1f) TurnBack();
    }

    void Hit(Vector2 point)
    {
        HitResolution.Deliver(target, owner, attack, point);
        HitResolution.NotifyAttacker(owner, attack, point);

        Place(point);
        stuckOffset = point - (Vector2)target.Owner.transform.position;
        stuckUntil = Time.time + cfg.stickTime;
        phase = Phase.Stuck;
    }

    void Stick()
    {
        if (target != null && target.Owner != null)
            Place((Vector2)target.Owner.transform.position + stuckOffset);
        if (Time.time >= stuckUntil) TurnBack();
    }

    void TurnBack()
    {
        backFrom = transform.position;
        backT = 0f;
        phase = Phase.Back;
    }

    void FlyBack(float dt)
    {
        if (hand == null)
        {
            Catch();
            return;
        }

        Vector2 to = held != null ? CenterOf(held) : (Vector2)hand.position;
        Vector2 control = Control(backFrom, to);
        float speed = cfg.returnSpeed * Mathf.Lerp(0.35f, 1.3f, backT);   // eases in: slow, then a whoosh
        backT = Mathf.Min(1f, backT + speed * dt / Length(backFrom, control, to));
        Vector2 pos = Bezier(backFrom, control, to, backT);

        Spin(dt);
        float settle = Mathf.InverseLerp(1f - SettleFraction, 1f, backT);
        if (settle > 0f && held != null)
        {
            spin.rotation = Quaternion.Slerp(spin.rotation, held.transform.rotation, settle);
            SetFlip(held.flipX);
        }
        Place(pos);

        if (backT >= 1f) Catch();
    }

    static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * c + t * t * b;
    }

    // The curve's length, roughly (along its control polygon): progress per frame.
    static float Length(Vector2 a, Vector2 c, Vector2 b) =>
        Mathf.Max(0.01f, Vector2.Distance(a, c) + Vector2.Distance(c, b));

    // A flight's control point: the chord's middle, swung out to `curveSide` of it.
    // Out and back run the chord opposite ways, so they swing to opposite sides.
    Vector2 Control(Vector2 from, Vector2 to)
    {
        Vector2 chord = to - from;
        Vector2 side = new Vector2(-chord.y, chord.x) * (curveSide * cfg.returnCurve);
        return (from + to) * 0.5f + side;
    }

    void Catch()
    {
        caught?.Invoke();
        Destroy(gameObject);
    }

    void Spin(float dt)
    {
        angle += cfg.spinSpeed * spinSign * dt;
        spin.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Move to `pos` (at hand height) and sort by the ground point under it.
    void Place(Vector2 pos)
    {
        transform.position = pos;
        float groundY = pos.y - height;
        if (ysort != null) ysort.ApplyAt(groundY);
        if (shadow != null)
        {
            shadow.transform.position = new Vector3(pos.x, groundY, 0f);
            shadow.sortingOrder = art.sortingOrder - 1;
        }
    }

    // Mirror the art, keeping its middle on `spin` (flipX mirrors about the pivot).
    void SetFlip(bool flip)
    {
        art.flipX = flip;
        Vector2 c = art.sprite != null ? (Vector2)art.sprite.bounds.center : Vector2.zero;
        art.transform.localPosition = new Vector3(flip ? c.x : -c.x, -c.y, 0f);
    }

    // World position of a renderer's sprite middle, honouring its flipX.
    static Vector2 CenterOf(SpriteRenderer r)
    {
        Vector2 c = r.sprite != null ? (Vector2)r.sprite.bounds.center : Vector2.zero;
        return r.transform.TransformPoint(new Vector3(r.flipX ? -c.x : c.x, c.y, 0f));
    }

    static bool Valid(Hurtbox hb) =>
        hb != null && hb.HurtCollider != null && hb.Health != null && !hb.Health.IsDead && hb.CanBeHit;

    // A small soft ellipse, for a Shadow with no sprite of its own.
    static Sprite DefaultShadow()
    {
        if (shadowSprite != null) return shadowSprite;
        const int w = 7, h = 3;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, dx * dx + dy * dy <= 1f ? 0.35f : 0f));
            }
        tex.Apply();
        return shadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
    }
}
