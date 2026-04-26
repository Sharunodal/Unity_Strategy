using UnityEngine;

public class CombatAnimationEvents : MonoBehaviour
{
    // Animation events in Unity can only call functions on the same GameObject, not children.
    // Thus we need this intermediary script to forward the calls to the WeaponHitbox component.
    [SerializeField] private WeaponHitbox weaponHitbox;
    [SerializeField] private UnitAnimator unitAnimator;
    [SerializeField] private UnitBrain unitBrain;
    private Unit unit;
    private BowWeapon bow;
    private int attackActionVersion;
    private bool hasAttackActionVersion;
    private bool comboContinuationChecked;
    private bool comboContinued;

    private void Awake()
    {
        if (weaponHitbox == null)
        {
            weaponHitbox = GetComponentInChildren<WeaponHitbox>();
        }
        if (unitAnimator == null)
        {
            unitAnimator = GetComponent<UnitAnimator>();
            if (unitAnimator == null)
            {
                unitAnimator = GetComponentInParent<UnitAnimator>();
            }
        }
        if (unitBrain == null)
        {
            unitBrain = GetComponent<UnitBrain>();
            if (unitBrain == null)
            {
                unitBrain = GetComponentInParent<UnitBrain>();
            }
        }
        unit = GetComponent<Unit>();
        if (unit == null)
        {
            unit = GetComponentInParent<Unit>();
        }
        bow = GetComponentInChildren<BowWeapon>(true);
    }

    public void BeginAttackAction()
    {
        if (unitAnimator == null)
            return;

        hasAttackActionVersion = false;
        CaptureAttackActionVersion();
        comboContinuationChecked = false;
        comboContinued = false;
    }

    public void EnableWeaponHitbox()
    {
        if (unitAnimator != null && !unitAnimator.IsCurrentMeleeAction)
            return;

        BeginAttackAction();
        weaponHitbox?.EnableHitbox();
    }

    public void DisableWeaponHitbox()
    {
        weaponHitbox?.DisableHitbox();
    }

    public void FinishAttack()
    {
        if (comboContinued)
        {
            ClearAttackActionVersion();
            return;
        }

        if (TryGetAttackActionVersion(out int actionVersion))
            unitAnimator?.FinishCurrentAction(actionVersion);

        ClearAttackActionVersion();
    }

    public void FireArrow()
    {
        if (unitAnimator != null && !unitAnimator.IsCurrentRangedAction)
            return;

        if (unit != null && !unit.IsRanged)
            return;

        if (!hasAttackActionVersion)
            CaptureAttackActionVersion();

        bow?.FireArrow();
    }

    public void TryContinueQueuedAttack()
    {
        if (!TryGetAttackActionVersion(out int actionVersion))
            return;

        if (unitAnimator == null || !unitAnimator.IsCurrentActionVersion(actionVersion))
            return;

        if (comboContinuationChecked)
            return;

        if (unitBrain != null && unitBrain.GetAutoComboToggled())
            unitBrain.RequestAttack();

        comboContinuationChecked = true;
        comboContinued = unitAnimator.TryContinueCurrentAction(actionVersion);
    }

    private void CaptureAttackActionVersion()
    {
        if (unitAnimator == null)
            return;

        attackActionVersion = unitAnimator.CurrentActionVersion;
        hasAttackActionVersion = true;
    }

    private bool TryGetAttackActionVersion(out int actionVersion)
    {
        actionVersion = attackActionVersion;

        if (!hasAttackActionVersion)
            return false;

        return true;
    }

    private void ClearAttackActionVersion()
    {
        attackActionVersion = 0;
        hasAttackActionVersion = false;
        comboContinuationChecked = false;
        comboContinued = false;
    }
}
