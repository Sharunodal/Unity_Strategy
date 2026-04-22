using System.Collections.Generic;
using UnityEngine;

public class UnitCommandUI : MonoBehaviour
{
    [SerializeField] private SelectionManager selectionManager;

    public void OnSwordButtonPressed()
    {
        GiveWeaponOrder(WeaponType.Sword);
    }

    public void OnBowButtonPressed()
    {
        GiveWeaponOrder(WeaponType.Bow);
    }

    public void OnToggleRunButtonPressed()
    {
        foreach (UnitCommandReceiver receiver in selectionManager.GetSelectedCommandReceivers())
        {
            if (receiver != null)
                receiver.ToggleRun();
        }
    }

    public void OnToggleBlockButtonPressed()
    {
        foreach (UnitCommandReceiver receiver in selectionManager.GetSelectedCommandReceivers())
        {
            if (receiver != null)
                receiver.ToggleBlocking();
        }
}

    private void GiveWeaponOrder(WeaponType weaponType)
    {
        foreach (UnitCommandReceiver receiver in selectionManager.GetSelectedCommandReceivers())
        {
            if (receiver != null)
                receiver.SetWeapon(weaponType);
        }
    }
}
