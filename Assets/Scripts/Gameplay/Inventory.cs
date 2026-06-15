using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // item type (e.g. "Wood") -> the actual carried objects
    Dictionary<string, List<GameObject>> items = new Dictionary<string, List<GameObject>>();

    public void Add(string item, GameObject obj)
    {
        if (!items.ContainsKey(item))
            items[item] = new List<GameObject>();
        items[item].Add(obj);
    }

    public int Count(string item)
    {
        return items.ContainsKey(item) ? items[item].Count : 0;
    }

    // remove ONE of a type and destroy that object (no-op if you have none)
    public void Remove(string item)
    {
        if (!items.ContainsKey(item) || items[item].Count == 0)
            return;

        List<GameObject> list = items[item];
        GameObject obj = list[^1];   // take the most recently added
        list.RemoveAt(list.Count - 1);
        Destroy(obj);
    }
}
