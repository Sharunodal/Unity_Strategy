using UnityEngine;

public class CombatAnimationEvents : MonoBehaviour
{
    // Animation events in Unity can only call functions on the same GameObject, not children.
    // Thus we need this intermediary script to forward the calls to the WeaponHitbox component.
    [SerializeField] private WeaponHitbox weaponHitbox;
    private BowWeapon bow;

    private void Awake()
    {
        if (weaponHitbox == null)
        {
            weaponHitbox = GetComponentInChildren<WeaponHitbox>();
        }
        bow = GetComponentInChildren<BowWeapon>(true);
    }

    public void BeginAttack()
    {
        weaponHitbox.BeginAttack();
    }

    public void EndAttack()
    {
        weaponHitbox.EndAttack();
    }

    public void FireArrow()
    {
        bow?.FireArrow();
    }
}
