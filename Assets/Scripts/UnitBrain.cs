using System;
using UnityEngine;
using UnityEngine.AI;

public enum UnitState { Idle, Moving, Attacking, RangeAttacking, Following, Blocking }

public class UnitBrain : MonoBehaviour
{
    [SerializeField] private float meleeRange = 3.0f;
    [SerializeField] private float attacksPerSecond = 1.0f;
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float staminaDrainRunning = 10f;
    [SerializeField] private float staminaRegen = 5f;
    [SerializeField] private float turnSpeedWhileInCombat = 12f;
    [SerializeField] private float meleeFacingAngle = 12f;

    [SerializeField] private float bowRange = 15f;
    [SerializeField] private float shotsPerSecond = 0.8f;
    [SerializeField] private float bowFacingAngle = 8f;

    private NavMeshAgent agent;
    private IUnitCommand currentCommand;

    private Unit self;
    private Unit attackTarget;
    private Unit followTarget;

    private bool runToggled = false;
    private bool blockToggled = false;
    private BlockController blockController;

    public event Action AttackTriggered;
    private float nextSwingTime = 0f;

    public event Action RangedTriggered;
    private float nextShotTime = 0f;

    private bool hasPendingCommand;
    private IUnitCommand pendingCommand;
    private Unit pendingAttackTarget;
    private Unit pendingFollowTarget;

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

            StopAll();
            return;
        }
        else
        {
            agent.isStopped = false;

            if (hasPendingCommand)
            {
                hasPendingCommand = false;

                // Rebuild references
                if (pendingCommand is AttackCommand)
                {
                    if (pendingAttackTarget != null)
                        ExecuteCommand(new AttackCommand(pendingAttackTarget));
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

    private void ExecuteCommand(IUnitCommand command)
    {
        currentCommand = command;

        attackTarget = null;
        followTarget = null;

        if (command is MoveCommand move)
        {
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

            agent.isStopped = false;
            agent.stoppingDistance = meleeRange;
            agent.SetDestination(attackTarget.transform.position);
            state = UnitState.Moving;
            return;
        }

        if (command is FollowCommand follow)
        {
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
            // Queue the latest command to run after block ends
            hasPendingCommand = true;
            pendingCommand = command;
            pendingAttackTarget = (command is AttackCommand a) ? a.Target : null;
            pendingFollowTarget = (command is FollowCommand f) ? f.Target : null;
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
            if (attackTarget == self)
            {
                currentCommand = null;
                attackTarget = null;
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

                float attackRate = hasBow ? shotsPerSecond : attacksPerSecond;
                float interval = 1f / Mathf.Max(0.01f, attackRate);
                float nextAttackTime = hasBow ? nextShotTime : nextSwingTime;
                if (Time.time >= nextAttackTime)
                {
                    if (hasBow)
                    {
                        nextShotTime = Time.time + interval;
                        RangedTriggered?.Invoke();
                    }
                    else
                    {
                        nextSwingTime = Time.time + interval;
                        AttackTriggered?.Invoke();
                    }
                }
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

        // Pending command is not cleared for now, as we want to be able to resume it after blocking

        agent.isStopped = true;
        agent.ResetPath();
        state = UnitState.Idle;
    }
}
