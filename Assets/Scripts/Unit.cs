using System;
using UnityEngine;

public enum WeaponType { None, Sword, Bow }

public class Unit : MonoBehaviour
{
    public string unitName = "DefaultUnit";
    public int ownerId = 0;
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

    [SerializeField] private WeaponType weapon = WeaponType.Sword;
    [SerializeField] private GameObject sword;
    [SerializeField] private GameObject bow;
    public WeaponType Weapon => weapon;
    public bool IsRanged => weapon == WeaponType.Bow;

    private void Awake()
    {
        EquipWeapon(weapon, force: true);
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

    public void SetHitpoints(float newValue)
    {
        float clamped = Mathf.Clamp(newValue, 0f, maxHitpoints);
        if (Mathf.Approximately(clamped, currentHitpoints))
            return;

        currentHitpoints = clamped;
        NotifyStatsChanged();
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
        SetHitpoints(currentHitpoints - damage);
        if (currentHitpoints <= 0f)
        {
            GetKnockedOut();
        }
    }

    private void GetKnockedOut()
    {
        Destroy(gameObject);
    }
}
