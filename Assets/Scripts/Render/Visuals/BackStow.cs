using UnityEngine;

// Shows the stowed weapons on the player's back, crossed as an X with their heads
// pointing down. While the hands carry a load, Hands stows the whole loadout
// (inactive: a torch gives no light, warmth or refuel prompt), so this only draws a
// copy of each active-set item's main sprite -- the Main Hand on `backMain`, the Off
// Hand on `backOff` -- and never touches the items themselves. A torch shows as its
// stick: its fire is a separate child.
//
// Rig: BackRig (a child of the player) is keyed in every player clip to the Body
// sprite's top-centre each frame (Tools/Player/Bake Back Rig), so the X bobs and
// leans with the body art. The two anchors under it hold the X's pose -- the
// crossing point and each diagonal's angle -- authored in the prefab; each one's
// Art child is centred on the item sprite, so any length of item crosses at the
// same point. The X flips between the back view (N: NE/NW, as authored) and the
// front view (S: SE/SW, mirrored -- the same X seen from the other side): the
// anchors mirror the way PlayerAnimator mirrors the hand rig (x -> -x, rotation
// conjugated, never a negative scale) and the art flips. Turning east <-> west
// doesn't flip it; only the rig's position follows the west mirror
// (PlayerAnimator.FlipX), since the mirrored body's centre moves to -x.
//
// Sorting: the player's SortingGroup makes orders relative to the body. Facing NE
// (back to the camera) the X draws over the back; facing SE, behind the body.
[RequireComponent(typeof(Hands))]
public class BackStow : MonoBehaviour
{
    [SerializeField] Transform backRig;
    [Tooltip("The Main Hand item's diagonal.")]
    [SerializeField] Transform backMain;
    [Tooltip("The Off Hand item's diagonal.")]
    [SerializeField] Transform backOff;

    [Tooltip("Source of facing and the west mirror. Falls back to a PlayerAnimator on this GameObject.")]
    [SerializeField] PlayerAnimator playerAnimator;
    [Tooltip("Body sprite the X sorts relative to. Falls back to a SpriteRenderer on this GameObject.")]
    [SerializeField] SpriteRenderer body;

    // Degrees the whole X turns about its crossing point (counter-clockwise is positive),
    // on top of the anchors' authored angles; mirrored with the X in the front view.
    [Header("Rotate")]
    [Tooltip("Facing east (NE/SE): degrees the whole X turns about its crossing point, counter-clockwise " +
             "is positive. Mirrored with the X in the front view. Tunes live.")]
    [SerializeField] float rotateEast = 0f;
    [Tooltip("Facing west (NW/SW): degrees the whole X turns about its crossing point, counter-clockwise " +
             "is positive. Mirrored with the X in the front view. Tunes live.")]
    [SerializeField] float rotateWest = 0f;

    [Tooltip("Order added to the body's facing NE (the back faces the camera): over the head, gear and bag.")]
    [SerializeField] int backViewOffset = 6;
    [Tooltip("Order added to the body's facing SE: behind the body.")]
    [SerializeField] int frontViewOffset = -1;

    // A mirrored transform: its unmirrored pose (the Animator's, or the prefab's), plus
    // the pose last written, to tell a fresh Animator pose from our own.
    class Mirrored
    {
        public Transform t;
        public Vector3 sourcePos, writtenPos;
        public Quaternion sourceRot, writtenRot;
        public bool primed;

        // `turn` rotates the unmirrored pose about z first (degrees, counter-clockwise),
        // so a mirrored copy turns the other way.
        public void Apply(bool flip, float turn = 0f)
        {
            if (t == null) return;
            Vector3 pos = t.localPosition;
            Quaternion rot = t.localRotation;
            if (!primed || pos != writtenPos || rot != writtenRot)
            {
                sourcePos = pos;
                sourceRot = rot;
            }
            Vector3 outPos = sourcePos;
            Quaternion outRot = turn != 0f ? Quaternion.Euler(0f, 0f, turn) * sourceRot : sourceRot;
            if (flip)
            {
                outPos.x = -outPos.x;
                outRot = new Quaternion(outRot.x, -outRot.y, -outRot.z, outRot.w);
            }
            t.localPosition = writtenPos = outPos;
            t.localRotation = writtenRot = outRot;
            primed = true;
        }
    }

    Hands hands;
    Loadout loadout;
    Mirrored rig, main, off;
    SpriteRenderer mainArt, offArt;

    void Awake()
    {
        hands = GetComponent<Hands>();
        loadout = GetComponent<Loadout>();
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        if (body == null) body = GetComponent<SpriteRenderer>();

        rig = new Mirrored { t = backRig };
        main = new Mirrored { t = backMain };
        off = new Mirrored { t = backOff };
        mainArt = backMain != null ? backMain.GetComponentInChildren<SpriteRenderer>(true) : null;
        offArt = backOff != null ? backOff.GetComponentInChildren<SpriteRenderer>(true) : null;
    }

    void LateUpdate()
    {
        bool west = playerAnimator != null && playerAnimator.FlipX;
        rig.Apply(west);
        float rotate = west ? rotateWest : rotateEast;

        // the back view (N) as authored, the front view (S) mirrored
        bool backView = playerAnimator != null && CardinalDir.Resolve(playerAnimator.Facing).dir == "ne";
        main.Apply(!backView, rotate);
        off.Apply(!backView, rotate);

        bool carrying = hands.IsCarrying;
        int order = (body != null ? body.sortingOrder : 0) + (backView ? backViewOffset : frontViewOffset);
        Show(mainArt, carrying ? loadout.Drawn(HandSide.Left) : null, !backView, order);
        // the Off Hand's art is mirrored, so the two items face away from each other
        Show(offArt, carrying ? loadout.Drawn(HandSide.Right) : null, backView, order);
    }

    // Draw `item`'s main sprite on an anchor's art, centred on the anchor; null hides it.
    static void Show(SpriteRenderer art, GameObject item, bool flip, int order)
    {
        if (art == null) return;
        SpriteRenderer source = item != null ? MainRenderer(item) : null;
        Sprite sprite = source != null ? source.sprite : null;
        if (sprite == null)
        {
            if (art.enabled) art.enabled = false;
            return;
        }

        if (art.sprite != sprite) art.sprite = sprite;
        art.sharedMaterial = source.sharedMaterial;
        art.flipX = flip;
        art.sortingOrder = order;
        // the sprite's middle on the anchor (flipX mirrors the art about its pivot)
        Vector3 c = sprite.bounds.center;
        art.transform.localPosition = new Vector3(flip ? c.x : -c.x, -c.y, 0f);
        if (!art.enabled) art.enabled = true;
    }

    // The item's own sprite, else its first child's -- the same pick as UiPixel.IconOf.
    static SpriteRenderer MainRenderer(GameObject item) =>
        item.TryGetComponent(out SpriteRenderer sr) ? sr : item.GetComponentInChildren<SpriteRenderer>(true);
}
