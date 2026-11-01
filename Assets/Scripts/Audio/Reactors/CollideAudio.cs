using UnityEngine;

// Walking into or through a prop, heard (sound.md §9.3, §7.3): a bush's frozen leaves, a
// hemp plant's stalks, a log knocking, the big stone scraping. Beside CollideAnimation on
// the same trigger, counting the same bodies (its EntityLayers), once per entering body
// at most every `cooldown` seconds -- there are 44 bushes, and a pack of wolves running
// through one shouldn't drum.
public class CollideAudio : MonoBehaviour
{
    public SoundEvent touched;
    [Min(0f)] public float cooldown = 0.5f;

    CollideAnimation collide;
    float last = float.NegativeInfinity;

    void Awake() => collide = GetComponent<CollideAnimation>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collide != null && ((1 << other.gameObject.layer) & collide.EntityLayers.value) == 0) return;
        if (Time.time - last < cooldown) return;
        last = Time.time;
        Sfx.Play(touched, transform.position);
    }
}
