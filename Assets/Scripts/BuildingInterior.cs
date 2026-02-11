using System.Collections.Generic;
using UnityEngine;

public class BuildingInterior : MonoBehaviour
{
    private readonly HashSet<Unit> inside = new();
    public bool IsInside(Unit u) => u != null && inside.Contains(u);

    private void OnTriggerEnter(Collider other)
    {
        var unit = other.GetComponentInParent<Unit>();
        if (unit != null)
            inside.Add(unit);
    }

    private void OnTriggerExit(Collider other)
    {
        var unit = other.GetComponentInParent<Unit>();
        if (unit != null)
            inside.Remove(unit);
    }
}
