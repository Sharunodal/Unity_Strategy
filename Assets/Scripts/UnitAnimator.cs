using UnityEngine;
using UnityEngine.AI;

public class UnitAnimator : MonoBehaviour
{
    private enum QueuedAnimatorAction
    {
        None,
        MeleeAttack,
        RangedAttack
    }

    [SerializeField] private Animator animator;
    [SerializeField] private string meleeAttackTrigger = "Attack";
    [SerializeField] private string rangedAttackTrigger = "ShootBow";
    [SerializeField] private string isAttackingBool = "IsAttacking";

    private UnitBrain brain;
    private NavMeshAgent agent;
    private BowWeapon bowWeaponScript;
    private int meleeAttackHash;
    private int rangedAttackHash;
    private int isAttackingHash;
    private QueuedAnimatorAction currentAction;
    private QueuedAnimatorAction queuedAction;
    private int currentActionVersion;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    public int CurrentActionVersion => currentActionVersion;
    public bool HasActiveAction => currentAction != QueuedAnimatorAction.None;
    public bool IsCurrentMeleeAction => currentAction == QueuedAnimatorAction.MeleeAttack;
    public bool IsCurrentRangedAction => currentAction == QueuedAnimatorAction.RangedAttack;

    private void Awake()
    {
        brain = GetComponent<UnitBrain>();
        agent = GetComponent<NavMeshAgent>();
        bowWeaponScript = GetComponentInChildren<BowWeapon>(true);
        meleeAttackHash = Animator.StringToHash(meleeAttackTrigger);
        rangedAttackHash = Animator.StringToHash(rangedAttackTrigger);
        isAttackingHash = Animator.StringToHash(isAttackingBool);
    }

    private void OnEnable()
    {
        brain.AttackTriggered += OnAttackTriggered;
        brain.RangedTriggered += OnRangedShot;
    }

    private void OnDisable()
    {
        brain.AttackTriggered -= OnAttackTriggered;
        brain.RangedTriggered -= OnRangedShot;
    }

    private void Update()
    {
        if (animator != null && agent != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    private void OnAttackTriggered()
    {
        RequestAction(QueuedAnimatorAction.MeleeAttack);
    }

    private void OnRangedShot()
    {
        if (bowWeaponScript != null)
            bowWeaponScript.SetTarget(brain.GetAttackTarget());

        RequestAction(QueuedAnimatorAction.RangedAttack);
    }

    public void FinishMeleeAttack()
    {
        FinishCurrentAction();
    }

    public bool TryContinueCurrentAction(int actionVersion)
    {
        if (actionVersion != currentActionVersion || currentAction == QueuedAnimatorAction.None)
            return false;

        if (queuedAction == QueuedAnimatorAction.None)
            return false;

        QueuedAnimatorAction nextAction = queuedAction;
        queuedAction = QueuedAnimatorAction.None;
        currentAction = QueuedAnimatorAction.None;
        StartAction(nextAction);
        return true;
    }

    public bool IsCurrentActionVersion(int actionVersion)
    {
        return actionVersion == currentActionVersion && currentAction != QueuedAnimatorAction.None;
    }

    public void InterruptCurrentAction()
    {
        queuedAction = QueuedAnimatorAction.None;
        currentAction = QueuedAnimatorAction.None;
        currentActionVersion++;

        if (animator == null)
            return;

        animator.ResetTrigger(meleeAttackHash);
        animator.ResetTrigger(rangedAttackHash);
        SetAttackChainActive(false);
    }

    public void FinishCurrentAction()
    {
        FinishCurrentAction(currentActionVersion);
    }

    public void FinishCurrentAction(int actionVersion)
    {
        if (actionVersion != currentActionVersion || currentAction == QueuedAnimatorAction.None)
            return;

        queuedAction = QueuedAnimatorAction.None;
        currentAction = QueuedAnimatorAction.None;
        SetAttackChainActive(false);
    }

    private void RequestAction(QueuedAnimatorAction action)
    {
        if (action == QueuedAnimatorAction.None || animator == null)
            return;

        if (currentAction == QueuedAnimatorAction.None)
        {
            StartAction(action);
            return;
        }

        queuedAction = action;
    }

    private void StartAction(QueuedAnimatorAction action)
    {
        if (action == QueuedAnimatorAction.None || animator == null)
            return;

        currentAction = action;
        currentActionVersion++;
        SetAttackChainActive(action != QueuedAnimatorAction.None);
        SetActionTrigger(action);
    }

    private void SetAttackChainActive(bool active)
    {
        if (animator != null)
            animator.SetBool(isAttackingHash, active);
    }

    private void SetActionTrigger(QueuedAnimatorAction action)
    {
        switch (action)
        {
            case QueuedAnimatorAction.MeleeAttack:
                animator.ResetTrigger(meleeAttackHash);
                animator.SetTrigger(meleeAttackHash);
                break;
            case QueuedAnimatorAction.RangedAttack:
                animator.ResetTrigger(rangedAttackHash);
                animator.SetTrigger(rangedAttackHash);
                break;
        }
    }
}
