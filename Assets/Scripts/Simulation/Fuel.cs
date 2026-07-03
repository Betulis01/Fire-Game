using System;
using System.Collections.Generic;
using UnityEngine;

// The fire's fuel as a FIFO queue of burning items, each with its own burn rate.
// The front item burns at its rate until spent, then the next takes over.
// fuelLevel (0..1) reflects the TOTAL remaining fuel, so light/heat scale with it.
public class Fuel : MonoBehaviour
{
    class BurningItem
    {
        public float remaining;
        public float burnRate;
    }

    public float maxFuel = 150f;        // remaining fuel that reads as "full" (0..1 scaling)
    public float startFuel = 15f;      // fuel the fire begins with (0 = starts out)
    public float startBurnRate = 1f;    // burn rate of that starting fuel

    readonly List<BurningItem> items = new();   // front (index 0) is burning now

    public event Action<float> FuelAdded;   // raised when fuel is added; passes the amount

    float Total
    {
        get
        {
            float t = 0f;
            foreach (BurningItem i in items) t += i.remaining;
            return t;
        }
    }

    public float fuelLevel => maxFuel > 0f ? Mathf.Clamp01(Total / maxFuel) : 0f;

    // burn rate of the item currently being consumed (0 when the fire is out)
    public float CurrentBurnRate => items.Count > 0 ? items[0].burnRate : 0f;

    void Awake()
    {
        if (startFuel > 0f)
            items.Add(new BurningItem { remaining = startFuel, burnRate = startBurnRate });
    }

    void Update()
    {
        // spend this frame's time across the queue so high rates / tiny remainders are exact
        float dt = Time.deltaTime;
        while (dt > 0f && items.Count > 0)
        {
            BurningItem cur = items[0];
            float life = cur.remaining / cur.burnRate;   // seconds until this item is gone

            if (life > dt)
            {
                cur.remaining -= cur.burnRate * dt;
                dt = 0f;
            }
            else
            {
                dt -= life;
                items.RemoveAt(0);
                if (items.Count == 0)
                    Debug.Log("The fire has gone out.");
            }
        }
    }

    // add a burning item (amount of fuel + how fast it burns)
    public void Add(float amount, float burnRate)
    {
        if (amount <= 0f) return;

        items.Add(new BurningItem { remaining = amount, burnRate = burnRate });
        FuelAdded?.Invoke(amount);
    }
}
