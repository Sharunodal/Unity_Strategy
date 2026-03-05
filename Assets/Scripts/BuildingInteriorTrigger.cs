using System.Collections.Generic;
using UnityEngine;

public class BuildingInteriorTrigger : MonoBehaviour
{
    private readonly HashSet<Unit> inside = new();
    public bool IsInside(Unit unit)
    {
        if (unit != null && inside.Contains(unit))
            return true;
        return false;
    }

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
