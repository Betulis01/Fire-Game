using UnityEngine;

// Victim-side reactor: the sound of this entity being struck, by what it's made of
// (SurfaceSounds: flesh, bone, wood, stone, ...), not by which weapon struck it -- the
// audio counterpart of SurfaceHitEffect, sitting beside it. Hurtbox fans every landed
// hit (melee, arrow, thrown axe, whirlwind) to it as an IHitReactor.
//   - a hit the striking tool can't damage (ToolDamageFilter) plays the glance knock
//   - a secondary (cleave) target plays 6 dB down, so a crowd doesn't stack up
//   - a crit adds AudioConfig.critLayer on top
//   - a hit the player's raised shield takes is the shield's sound (WeaponAudio), not this
public class SurfaceAudio : MonoBehaviour, IHitReactor
{
    public SurfaceSounds surface;

    ToolDamageFilter filter;
    ShieldBlock shield;

    void Awake()
    {
        filter = GetComponent<ToolDamageFilter>();
        shield = GetComponent<ShieldBlock>();
    }

    public void OnHit(in HitInfo hit)
    {
        if (surface == null) return;
        if (shield != null && shield.IsBlocking) return;

        bool glance = filter != null && !filter.Accepts(hit.kind);
        SoundEvent e = glance ? surface.glance : surface.impact;
        float volume = hit.isPrimary ? 1f : 0.5f;
        Sfx.Play(e, hit.point, volume);

        AudioConfig config = AudioSystem.ActiveConfig;
        if (hit.isCrit && !glance && config != null) Sfx.Play(config.critLayer, hit.point, volume);
    }
}
