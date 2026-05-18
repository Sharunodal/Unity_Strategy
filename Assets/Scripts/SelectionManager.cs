using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public event Action SelectionChanged;

    private readonly List<Selectable> selected = new();
    public IReadOnlyList<Selectable> Selected => selected;

    // Clears the selection and optionally notifies listeners.
    public void ClearSelection()
    {
        ClearSelection(true);
    }

    private void ClearSelection(bool notify)
    {
        for (int i = selected.Count - 1; i >= 0; i--)
        {
            var s = selected[i];
            if (s != null)
            {
                s.Destroyed -= OnSelectableDestroyed;
                s.SetSelection(false);
            }
            selected.RemoveAt(i);
        }

        if (notify)
            SelectionChanged?.Invoke();
    }

    public void SelectSingle(Selectable selectable, bool additive)
    {
        if (selectable == null) return;

        if (!additive)
            ClearSelection(false);

        if (!selected.Contains(selectable))
        {
            selected.Add(selectable);
            selectable.Destroyed += OnSelectableDestroyed;
            selectable.SetSelection(true);
            SelectionChanged?.Invoke();
        }
    }

    public void SelectMultiple(IEnumerable<Selectable> selectables, bool additive)
    {
        if (!additive)
            ClearSelection(false);

        bool changed = !additive;

        foreach (Selectable selectable in selectables)
        {
            if (selectable == null || selected.Contains(selectable))
                continue;

            selected.Add(selectable);
            selectable.Destroyed += OnSelectableDestroyed;
            selectable.SetSelection(true);
            changed = true;
        }

        if (changed)
            SelectionChanged?.Invoke();
    }

    private void OnSelectableDestroyed(Selectable selectable)
    {
        selected.Remove(selectable);
        SelectionChanged?.Invoke();
    }

    public bool IsSelected(Selectable selectable)
    {
        return selected.Contains(selectable);
    }

    public IEnumerable<UnitCommandReceiver> GetSelectedCommandReceivers()
    {
        // Remove destroyed selectables first
        for (int i = selected.Count - 1; i >= 0; i--)
        {
            if (selected[i] == null)
                selected.RemoveAt(i);
        }

        foreach (var selectable in selected)
        {
            if (selectable == null) // extra safety
                continue;

            var receiver = selectable.GetComponent<UnitCommandReceiver>();
            if (receiver != null)
                yield return receiver;
        }
    }
}
