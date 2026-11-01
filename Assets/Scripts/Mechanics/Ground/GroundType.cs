using System;
using UnityEngine;

// What an entity stands on (TerrainMap.At): the base ground everywhere (Snow), or the
// Ground Detail patch under the feet. Not "Terrain", which is Unity's own class.
public enum GroundType { Snow, Grass, Ice, Stone, Dirt }

// How a ground type moves whoever stands on it (TerrainMap's table, read through
// GroundSensor). Speed multiplies with the mover's other multipliers (cold, food,
// carrying); slipTime is the traction: seconds the velocity takes to ease toward the
// input (0 = instant, the normal feel); knockbackSlide stretches a shove's decay.
[Serializable]
public struct GroundProps
{
    public GroundType type;
    [Tooltip("Speed multiplier; stacks with cold, food buffs and the carrying penalty.")]
    public float speed;
    [Tooltip("Seconds the velocity takes to ease toward the input (and to a stop). 0 = instant grip.")]
    public float slipTime;
    [Tooltip("Knockback decay time multiplier: 3 = a shove carries about three times as long.")]
    public float knockbackSlide;
    [Tooltip("Whether walking here presses a snow trench (SnowTrail).")]
    public bool leavesTrail;

    public GroundProps(GroundType type, float speed, float slipTime, float knockbackSlide, bool leavesTrail)
    {
        this.type = type;
        this.speed = speed;
        this.slipTime = slipTime;
        this.knockbackSlide = knockbackSlide;
        this.leavesTrail = leavesTrail;
    }

    // a scene without a TerrainMap: no effect at all
    public static readonly GroundProps Neutral = new(GroundType.Snow, 1f, 0f, 1f, true);

    public static GroundProps[] Defaults() => new[]
    {
        new GroundProps(GroundType.Snow, 0.7f, 0f, 1f, true),
        new GroundProps(GroundType.Grass, 1f, 0f, 1f, false),
        new GroundProps(GroundType.Ice, 1f, 0.6f, 3f, false),
        new GroundProps(GroundType.Stone, 1f, 0f, 1f, false),
        new GroundProps(GroundType.Dirt, 1f, 0f, 1f, false),
    };
}
