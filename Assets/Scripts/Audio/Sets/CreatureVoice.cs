using UnityEngine;

// A creature type's voice and body (sound.md §6): one asset per kind (skeleton, wolf,
// boar, deer, polar bear), played by CreatureAudio. Footsteps are FootstepAudio's;
// the hit's impact is SurfaceAudio's. Empty fields stay silent.
[CreateAssetMenu(menuName = "Audio/Creature Voice", fileName = "Voice_")]
public class CreatureVoice : ScriptableObject
{
    [Header("Life")]
    public SoundEvent spawn;
    public SoundEvent idle;
    [Tooltip("Seconds between idle barks while wandering, random in this range (the global bark budget can delay one).")]
    public Vector2 idleInterval = new(5f, 12f);
    public SoundEvent despawn;

    [Header("Fight or flight")]
    public SoundEvent aggro;
    [Tooltip("Under the steps while chasing (the wolf's panting).")]
    public SoundEvent chaseLoop;
    public SoundEvent flee;
    [Tooltip("While fleeing (hoof drumming, panicked huffs).")]
    public SoundEvent fleeLoop;

    [Header("Attack")]
    [Tooltip("The windup starting: the dodge tell.")]
    public SoundEvent attackWindup;
    public SoundEvent attackSwing;
    [Tooltip("The contact frame, landed or not (the wolf's jaw snap).")]
    public SoundEvent attackHit;

    [Header("Hurt")]
    public SoundEvent hurt;
    [Tooltip("Hurt while burning. Empty = the normal hurt.")]
    public SoundEvent hurtBurning;
    public SoundEvent death;
    public SoundEvent knockback;
}
