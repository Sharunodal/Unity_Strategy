using System;

[Serializable]
public class UnitSaveData
{
    public string persistentId;
    public string unitName;
    public int ownerId;
    public float currentHitpoints;
    public float maxHitpoints;
    public float currentStamina;
    public float maxStamina;
    public float minStaminaToRun;
    public float currentHunger;
    public float maxHunger;
    public float attackDamage;
    public float walkSpeed;
    public float runSpeed;
    public WeaponType weapon;

    public UnitSaveData()
    {
    }

    public UnitSaveData(Unit unit)
    {
        Capture(unit);
    }

    public void Capture(Unit unit)
    {
        if (unit == null)
            return;

        persistentId = unit.PersistentId;
        unitName = unit.unitName;
        ownerId = unit.ownerId;
        currentHitpoints = unit.currentHitpoints;
        maxHitpoints = unit.maxHitpoints;
        currentStamina = unit.currentStamina;
        maxStamina = unit.maxStamina;
        minStaminaToRun = unit.minStaminaToRun;
        currentHunger = unit.currentHunger;
        maxHunger = unit.maxHunger;
        attackDamage = unit.attackDamage;
        walkSpeed = unit.walkSpeed;
        runSpeed = unit.runSpeed;
        weapon = unit.Weapon;
    }

    public bool Matches(Unit unit)
    {
        if (unit == null)
            return false;

        string unitId = unit.PersistentId;
        return !string.IsNullOrWhiteSpace(persistentId)
            && !string.IsNullOrWhiteSpace(unitId)
            && persistentId == unitId;
    }
}
