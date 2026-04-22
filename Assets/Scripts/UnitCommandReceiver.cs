using UnityEngine;

public class UnitCommandReceiver : MonoBehaviour
{
    private Unit unit;
    private UnitBrain brain;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        brain = GetComponent<UnitBrain>();
    }

    public void SetCommand(IUnitCommand command)
    {
        brain.SetCommand(command);
    }

    public void SetWeapon(WeaponType weaponType)
    {
        unit.EquipWeapon(weaponType);
    }

    public void ToggleRun()
    {
        brain.SetRunToggled(!brain.GetRunToggled());
    }

    public void SetBlocking(bool enabled)
    {
        brain.SetBlockToggled(enabled);
    }

    public void ToggleBlocking()
    {
        brain.SetBlockToggled(!brain.GetBlockToggled());
    }
}
