using System;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    public Unit unit;
    [SerializeField] private GameObject selectionIndicator;

    public event Action<Selectable> Destroyed;

    void Awake()
    {
        SetSelection(false);
    }

    public Unit GetUnit()
    {
        return unit;
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }

    public bool IsOwnedBy(int playerId)
    {
        return unit != null && unit.ownerId == playerId;
    }

    public void SetSelection(bool selected)
    {
        if (selectionIndicator)
        {
            selectionIndicator.SetActive(selected);
        }
    }
}
