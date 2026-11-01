using UnityEngine;

// An arrow, heard (sound.md §4.2): a whizz that follows it in flight and fades as it
// slows, a point-impact "thock" when it strikes (the victim's material plays too, through
// its SurfaceAudio), and a soft drop into the snow when it runs out of range.
[RequireComponent(typeof(Projectile))]
public class ProjectileAudio : MonoBehaviour
{
    public SoundEvent flightLoop;
    public SoundEvent hit;
    public SoundEvent drop;

    Projectile projectile;
    SoundHandle flight;
    Vector2 lastPos;
    float topSpeed;

    void Awake() => projectile = GetComponent<Projectile>();

    void OnEnable()
    {
        projectile.Launched += OnLaunched;
        projectile.HitSomething += OnHit;
        projectile.Expired += OnExpired;
    }

    void OnDisable()
    {
        projectile.Launched -= OnLaunched;
        projectile.HitSomething -= OnHit;
        projectile.Expired -= OnExpired;
        flight.Stop(0.05f);
    }

    void OnLaunched()
    {
        lastPos = transform.position;
        if (flightLoop != null) flight = Sfx.PlayLoop(flightLoop, transform);
    }

    // the whizz follows the arrow's speed: full at launch, dying as it eases to a stop
    void FixedUpdate()
    {
        if (!flight.IsValid) return;
        Vector2 pos = transform.position;
        float speed = (pos - lastPos).magnitude / Time.fixedDeltaTime;
        lastPos = pos;
        topSpeed = Mathf.Max(topSpeed, speed);
        if (topSpeed > 0f) flight.SetVolume(speed / topSpeed, 0.05f);
    }

    void OnHit(Vector2 point)
    {
        flight.Stop(0.03f);
        Sfx.Play(hit, point);
    }

    void OnExpired(Vector2 point)
    {
        flight.Stop(0.05f);
        Sfx.Play(drop, point);
    }
}
