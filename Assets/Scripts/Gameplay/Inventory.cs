using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // item type (e.g. "Wood") -> the actual carried objects
    Dictionary<string, List<GameObject>> items = new Dictionary<string, List<GameObject>>();

    public void Add(string type, GameObject obj)
    {
        if (!items.ContainsKey(type))
            items[type] = new List<GameObject>();
        items[type].Add(obj);
    }

    public int Count(string type)
    {
        return items.ContainsKey(type) ? items[type].Count : 0;
    }

    // remove ONE of a type and destroy that object (no-op if you have none)
    public void Remove(string type)
    {
        if (!items.ContainsKey(type) || items[type].Count == 0)
            return;

        List<GameObject> list = items[type];
        GameObject obj = list[^1];   // take the most recently added
        list.RemoveAt(list.Count - 1);
        Destroy(obj);
    }
}