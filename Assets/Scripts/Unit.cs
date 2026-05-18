using System;
using UnityEngine;

public enum WeaponType { None, Sword, Bow }

public class Unit : MonoBehaviour
{
    [SerializeField] private string persistentId;

    public string unitName = "DefaultUnit";
    public int ownerId = FactionRelations.Player1FactionId;
    public float currentHitpoints = 100f;
    public float maxHitpoints = 100f;
    public float currentStamina = 100f;
    public float maxStamina = 100f;
    public float minStaminaToRun = 20f;
    public float currentHunger = 100f;
    public float maxHunger = 100f;
    public float attackDamage = 10f;
    public float walkSpeed = 3.5f;
    public float runSpeed = 7.0f;

    public event Action statsChanged;
    public event Action<Unit, Unit> Damaged;

    [SerializeField] private FactionUnitTracker tracker;
    [SerializeField] private WeaponType weapon = WeaponType.Sword;
    [SerializeField] private GameObject sword;
    [SerializeField] private GameObject bow;
    public WeaponType Weapon => weapon;
    public bool IsRanged => weapon == WeaponType.Bow;
    public string PersistentId => string.IsNullOrWhiteSpace(persistentId) ? unitName : persistentId;
    private bool registeredWithTracker;

    private void Awake()
    {
        EquipWeapon(weapon, force: true);
    }

    private void OnEnable()
    {
        RegisterWithTracker();
    }

    private void OnDestroy()
    {
        UnregisterFromTracker();
    }

    private void NotifyStatsChanged()
    {
        statsChanged?.Invoke();
    }

    public void EquipWeapon(WeaponType newWeapon, bool force = false)
    {
        if (!force && weapon == newWeapon)
            return;

        weapon = newWeapon;

        if (sword)
            sword.SetActive(weapon == WeaponType.Sword);
        if (bow)
            bow.SetActive(weapon == WeaponType.Bow);
    }

    public UnitSaveData CreateSaveData()
    {
        return new UnitSaveData(this);
    }

    public void ApplySaveData(UnitSaveData saveData)
    {
        if (saveData == null)
            return;

        if (!string.IsNullOrWhiteSpace(saveData.persistentId))
            persistentId = saveData.persistentId;

        unitName = saveData.unitName;
        SetOwnerId(saveData.ownerId);
        maxHitpoints = saveData.maxHitpoints;
        currentHitpoints = Mathf.Clamp(saveData.currentHitpoints, 0f, maxHitpoints);
        maxStamina = saveData.maxStamina;
        currentStamina = Mathf.Clamp(saveData.currentStamina, 0f, maxStamina);
        minStaminaToRun = saveData.minStaminaToRun;
        maxHunger = saveData.maxHunger;
        currentHunger = Mathf.Clamp(saveData.currentHunger, 0f, maxHunger);
        attackDamage = saveData.attackDamage;
        walkSpeed = saveData.walkSpeed;
        runSpeed = saveData.runSpeed;
        EquipWeapon(saveData.weapon, force: true);

        UnitBrain brain = GetComponent<UnitBrain>();
        if (brain != null)
            brain.ApplySpeed();

        NotifyStatsChanged();
    }

    public void SetHitpoints(float newValue)
    {
        float clamped = Mathf.Clamp(newValue, 0f, maxHitpoints);
        if (Mathf.Approximately(clamped, currentHitpoints))
            return;

        currentHitpoints = clamped;
        NotifyStatsChanged();
    }

    public void SetOwnerId(int newOwnerId)
    {
        newOwnerId = FactionRelations.UpgradeLegacyPlayerFactionId(newOwnerId);

        if (ownerId == newOwnerId)
            return;

        ownerId = newOwnerId;

        if (registeredWithTracker && tracker != null)
            tracker.ChangeUnitFaction(this, ownerId);

        NotifyStatsChanged();
    }

    public void SetTracker(FactionUnitTracker newTracker)
    {
        if (tracker == newTracker)
        {
            RegisterWithTracker();
            return;
        }

        UnregisterFromTracker();
        tracker = newTracker;
        RegisterWithTracker();
    }

    public void SetStamina(float newValue)
    {
        float clamped = Mathf.Clamp(newValue, 0f, maxStamina);
        if (Mathf.Approximately(clamped, currentStamina))
            return;
        currentStamina = clamped;
        NotifyStatsChanged();
    }

    public void SetHunger(float newValue)
    {
        float clamped = Mathf.Clamp(newValue, 0f, maxHunger);
        if (Mathf.Approximately(clamped, currentHunger))
            return;
        currentHunger = clamped;
        NotifyStatsChanged();
    }

    public void TakeDamage(float damage, Unit attacker)
    {
        float previousHitpoints = currentHitpoints;
        SetHitpoints(currentHitpoints - damage);

        if (attacker != null && currentHitpoints < previousHitpoints)
        {
            Damaged?.Invoke(this, attacker);
        }

        if (currentHitpoints <= 0f)
        {
            GetKnockedOut();
        }
    }

    private void GetKnockedOut()
    {
        UnregisterFromTracker();
        Destroy(gameObject);
    }

    private void RegisterWithTracker()
    {
        if (registeredWithTracker || currentHitpoints <= 0f)
            return;

        if (tracker == null)
            return;

        tracker.RegisterUnit(this);
        registeredWithTracker = true;
    }

    private void UnregisterFromTracker()
    {
        if (!registeredWithTracker || tracker == null)
            return;

        tracker.UnregisterUnit(this);
        registeredWithTracker = false;
    }
}
