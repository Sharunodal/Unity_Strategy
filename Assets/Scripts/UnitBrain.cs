using System;
using UnityEngine;
using UnityEngine.AI;

public enum UnitState { Idle, Moving, Attacking, RangeAttacking, Following, Blocking }

public class UnitBrain : MonoBehaviour
{
    [SerializeField] private float meleeRange = 3.0f;
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float staminaDrainRunning = 10f;
    [SerializeField] private float staminaRegen = 5f;
    [SerializeField] private float turnSpeedWhileInCombat = 12f;
    [SerializeField] private float meleeFacingAngle = 12f;

    [SerializeField] private float bowRange = 15f;
    [SerializeField] private float bowFacingAngle = 8f;

    private NavMeshAgent agent;
    private IUnitCommand currentCommand;

    private Unit self;
    private UnitAnimator unitAnimator;
    private WeaponHitbox weaponHitbox;
    private Unit attackTarget;
    private Unit followTarget;

    private bool runToggled = false;
    private bool blockToggled = false;
    [SerializeField] private bool autoComboToggled = false;
    private BlockController blockController;

    public event Action AttackTriggered;
    private bool attackRequested;

    public event Action RangedTriggered;

    private bool hasPendingCommand;
    private IUnitCommand pendingCommand;
    private Unit pendingAttackTarget;
    private Unit pendingFollowTarget;
    private bool pendingCommandWasIssuedWhileBlocking;

    [SerializeField] private UnitState state = UnitState.Idle;

    public UnitState GetState()
    {
        return state;
    }

    public Unit GetAttackTarget()
    {
        return attackTarget;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        self = GetComponent<Unit>();
        unitAnimator = GetComponent<UnitAnimator>();
        weaponHitbox = GetComponentInChildren<WeaponHitbox>();
        blockController = GetComponent<BlockController>();
        ApplySpeed();
    }

    public void SetRunToggled(bool enabled)
    {
        runToggled = enabled;
        ApplySpeed();
    }

    public void SetBlockToggled(bool enabled)
    {
        if (blockToggled == enabled)
            return;

        blockToggled = enabled;

        if (blockController != null)
            blockController.SetBlocking(blockToggled);

        if (blockToggled)
        {
            hasPendingCommand = currentCommand != null;
            pendingCommand = currentCommand;
            pendingAttackTarget = attackTarget;
            pendingFollowTarget = followTarget;
            pendingCommandWasIssuedWhileBlocking = false;

            InterruptCurrentAttackAction();

            StopAll();
            return;
        }
        else
        {
            agent.isStopped = false;

            if (hasPendingCommand)
            {
                hasPendingCommand = false;
                bool commandWasIssuedWhileBlocking = pendingCommandWasIssuedWhileBlocking;
                pendingCommandWasIssuedWhileBlocking = false;

                // Rebuild references
                if (pendingCommand is AttackCommand)
                {
                    if (pendingAttackTarget != null)
                    {
                        bool shouldRequestAttack = commandWasIssuedWhileBlocking || autoComboToggled;
                        ExecuteCommand(new AttackCommand(pendingAttackTarget), shouldRequestAttack);
                    }
                }
                else if (pendingCommand is FollowCommand)
                {
                    if (pendingFollowTarget != null)
                        ExecuteCommand(new FollowCommand(pendingFollowTarget));
                }
                else
                {
                    ExecuteCommand(pendingCommand); // MoveCommand safe
                }
            }
        }
    }

    public bool GetRunToggled()
    {
        return runToggled;
    }

    public bool GetBlockToggled()
    {
        return blockToggled;
    }

    public void SetAutoComboToggled(bool enabled)
    {
        autoComboToggled = enabled;
    }

    public bool GetAutoComboToggled()
    {
        return autoComboToggled;
    }

    public void RequestAttack()
    {
        if (blockToggled)
        {
            Unit target = GetBlockingAttackTarget();
            if (target == null || target == self || target.currentHitpoints <= 0f)
                return;

            StopBlockingForImmediateAttack();
            ExecuteCommand(new AttackCommand(target));
            return;
        }

        if (attackTarget == null || attackTarget == self || attackTarget.currentHitpoints <= 0f)
            return;

        attackRequested = true;
        TryTriggerAttackRequest();
    }

    public void SetWeapon(WeaponType weaponType)
    {
        if (self == null)
            return;

        if (self.Weapon == weaponType)
            return;

        bool shouldResumeAttack = currentCommand is AttackCommand
            && attackTarget != null
            && attackTarget != self
            && attackTarget.currentHitpoints > 0f;

        InterruptCurrentAttackAction();

        self.EquipWeapon(weaponType);

        if (shouldResumeAttack)
        {
            agent.stoppingDistance = self.IsRanged ? bowRange : meleeRange;
            RequestAttack();
        }
    }

    public void ApplySpeed()
    {
        bool canRun = self.currentStamina >= self.minStaminaToRun;
        agent.speed = (runToggled && canRun) ? self.runSpeed : self.walkSpeed;
    }

    private void UpdateStamina(float dt)
    {
        bool isMoving = agent.velocity.sqrMagnitude > 0.05f;

        bool canRun = self.currentStamina >= self.minStaminaToRun;
        bool isRunningNow = runToggled && canRun;

        if (isMoving && isRunningNow)
        {
            self.SetStamina(self.currentStamina - staminaDrainRunning * dt);

            if (self.currentStamina < self.minStaminaToRun)
                runToggled = false;
        }
        else
        {
            self.SetStamina(self.currentStamina + staminaRegen * dt);
        }
    }

    private void ExecuteCommand(IUnitCommand command, bool requestAttackOnAttackCommand = true)
    {
        currentCommand = command;

        attackTarget = null;
        followTarget = null;

        if (command is MoveCommand move)
        {
            attackRequested = false;
            agent.isStopped = false;
            agent.stoppingDistance = 0f;
            agent.SetDestination(move.Destination);
            state = UnitState.Moving;
            return;
        }

        if (command is AttackCommand attack)
        {
            attackTarget = attack.Target;

            if (attackTarget == null || attackTarget == self)
            {
                currentCommand = null;
                attackTarget = null;
                state = UnitState.Idle;
                return;
            }

            if (requestAttackOnAttackCommand)
                RequestAttack();
            else
                attackRequested = false;

            agent.isStopped = false;
            agent.stoppingDistance = self.IsRanged ? bowRange : meleeRange;
            agent.SetDestination(attackTarget.transform.position);
            state = UnitState.Moving;
            return;
        }

        if (command is FollowCommand follow)
        {
            attackRequested = false;
            followTarget = follow.Target;

            if (followTarget == null || followTarget == self)
            {
                currentCommand = null;
                followTarget = null;
                state = UnitState.Idle;
                return;
            }

            agent.isStopped = false;
            agent.stoppingDistance = followDistance;
            agent.SetDestination(followTarget.transform.position);
            state = UnitState.Following;
            return;
        }

        currentCommand = null;
        state = UnitState.Idle;
    }

    public void SetCommand(IUnitCommand command)
    {
        if (blockToggled)
        {
            // Allow breaking block and attacking immediately without having to toggle block off manually
            if (command is AttackCommand attack)
            {
                // If the new attack target is the same as the one while blocking, break block
                if (attack.Target == GetBlockingAttackTarget())
                {
                    StopBlockingForImmediateAttack();
                    ExecuteCommand(new AttackCommand(attack.Target));
                    return;
                }

                // Otherwise, queue the new command and face the target while still blocking
                hasPendingCommand = true;
                pendingCommand = new AttackCommand(attack.Target);
                pendingAttackTarget = attack.Target;
                pendingFollowTarget = null;
                pendingCommandWasIssuedWhileBlocking = false;
                return;
            }

            // Queue the latest command to run after block ends
            hasPendingCommand = true;
            pendingCommand = command;
            pendingAttackTarget = (command is AttackCommand a) ? a.Target : null;
            pendingFollowTarget = (command is FollowCommand f) ? f.Target : null;
            pendingCommandWasIssuedWhileBlocking = true;
            return;
        }

        // Normal execution path
        ExecuteCommand(command);
    }

    private bool RotateTowardsTarget(Vector3 targetPos, float dt, float facingAngle)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return true;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnSpeedWhileInCombat * dt);

        // Check facing angle
        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= facingAngle;
    }

    private void Update()
    {
        UpdateStamina(Time.deltaTime);
        ApplySpeed();

        if (blockToggled)
        {
            // Stay frozen
            agent.isStopped = true;

            // Face target if we had an attack/follow command
            Unit faceTarget = null;

            if (hasPendingCommand && pendingCommand is AttackCommand)
                faceTarget = pendingAttackTarget;
            else if (hasPendingCommand && pendingCommand is FollowCommand)
                faceTarget = pendingFollowTarget;

            if (faceTarget != null)
                RotateTowardsTarget(faceTarget.transform.position, Time.deltaTime, meleeFacingAngle);

            state = UnitState.Blocking;
            return;
        }

        if (currentCommand == null)
        {
            state = UnitState.Idle;
            return;
        }

        if (followTarget != null)
        {
            if (followTarget == self)
            {
                currentCommand = null;
                followTarget = null;
                state = UnitState.Idle;
                return;
            }

            agent.isStopped = false;
            agent.stoppingDistance = followDistance;
            agent.SetDestination(followTarget.transform.position);

            float dist = Vector3.Distance(transform.position, followTarget.transform.position);
            state = (dist <= followDistance + 0.1f) ? UnitState.Idle : UnitState.Following;
            return;
        }

        if (attackTarget != null)
        {
            if (attackTarget == self || attackTarget.currentHitpoints <= 0f)
            {
                currentCommand = null;
                attackTarget = null;
                attackRequested = false;
                state = UnitState.Idle;
                return;
            }

            bool hasBow = self.IsRanged;
            float attackRange = hasBow ? bowRange : meleeRange + 0.25f;
            float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);

            if (distanceToTarget <= attackRange)
            {
                agent.isStopped = true;
                agent.ResetPath();

                float facingAngle = hasBow ? bowFacingAngle : meleeFacingAngle;
                bool facing = RotateTowardsTarget(attackTarget.transform.position, Time.deltaTime, facingAngle);

                state = hasBow ? UnitState.RangeAttacking : UnitState.Attacking;

                // Don't attack until facing the target
                if (!facing)
                    return;

                if (autoComboToggled && !attackRequested && (unitAnimator == null || !unitAnimator.HasActiveAction))
                    RequestAttack();

                TryTriggerAttackRequest();
            }
            else
            {
                // Chase
                agent.isStopped = false;
                agent.stoppingDistance = attackRange;
                agent.SetDestination(attackTarget.transform.position);
                state = UnitState.Moving;
            }

            return;
        }

        if (currentCommand is MoveCommand)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                currentCommand = null;
                state = UnitState.Idle;
            }
            else
            {
                state = UnitState.Moving;
            }
        }
        else
        {
            currentCommand = null;
            state = UnitState.Idle;
        }
    }

    public void StopAll()
    {
        currentCommand = null;
        attackTarget = null;
        followTarget = null;
        attackRequested = false;

        // Pending command is not cleared for now, as we want to be able to resume it after blocking

        agent.isStopped = true;
        agent.ResetPath();
        state = UnitState.Idle;
    }

    private void StopBlockingForImmediateAttack()
    {
        blockToggled = false;
        hasPendingCommand = false;
        pendingCommandWasIssuedWhileBlocking = false;

        if (blockController != null)
            blockController.SetBlocking(false);

        agent.isStopped = false;
    }

    private Unit GetBlockingAttackTarget()
    {
        if (hasPendingCommand && pendingCommand is AttackCommand)
            return pendingAttackTarget;

        return attackTarget;
    }

    private void InterruptCurrentAttackAction()
    {
        attackRequested = false;
        unitAnimator?.InterruptCurrentAction();
        weaponHitbox?.DisableHitbox();
    }

    private bool TryTriggerAttackRequest()
    {
        if (!attackRequested || self == null)
            return false;

        if (attackTarget == null || attackTarget == self || attackTarget.currentHitpoints <= 0f)
            return false;

        bool hasBow = self.IsRanged;
        float attackRange = hasBow ? bowRange : meleeRange + 0.25f;
        float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (distanceToTarget > attackRange)
            return false;

        float facingAngle = hasBow ? bowFacingAngle : meleeFacingAngle;
        if (!IsFacingTarget(attackTarget.transform.position, facingAngle))
            return false;

        attackRequested = false;

        if (hasBow)
            RangedTriggered?.Invoke();
        else
            AttackTriggered?.Invoke();

        return true;
    }

    private bool IsFacingTarget(Vector3 targetPos, float facingAngle)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return true;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= facingAngle;
    }
}
