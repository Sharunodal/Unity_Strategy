using UnityEngine;

public class EnemyUnitAI : MonoBehaviour
{
    [SerializeField] private float senseRadius = 12f;
    [SerializeField] private float assistRadius = 12f;
    [SerializeField] private float thinkInterval = 0.25f;
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private bool autoComboEnabled = true;

    private Unit self;
    private UnitBrain brain;

    private Unit currentTarget;
    private float nextThinkTime;

    private void Awake()
    {
        self = GetComponent<Unit>();
        brain = GetComponent<UnitBrain>();
    }

    private void OnEnable()
    {
        self.Damaged += OnDamaged;
        brain.SetAutoComboToggled(autoComboEnabled);
    }

    private void OnDisable()
    {
        self.Damaged -= OnDamaged;
    }

    private void Update()
    {
        if (Time.time < nextThinkTime)
            return;
        nextThinkTime = Time.time + thinkInterval;
        Think();
    }

    private void OnDamaged(Unit damagedUnit, Unit attacker)
    {
        if (attacker == null || attacker.ownerId == self.ownerId || attacker.currentHitpoints <= 0)
            return;

        if (currentTarget == null || currentTarget.currentHitpoints <= 0)
        {
            EngageTarget(attacker);
        }

        Collider[] allies = Physics.OverlapSphere(transform.position, assistRadius, unitLayer);
        foreach (var allyCollider in allies)
        {
            var allyUnit = allyCollider.GetComponent<Unit>();
            if (allyUnit == null || allyUnit == self || allyUnit.ownerId != self.ownerId)
                continue;

            var allyAI = allyUnit.GetComponent<EnemyUnitAI>();
            if (allyAI == null)
            {
                continue;
            }

            allyAI.TryAssistAgainst(attacker);
        }
    }

    private void Think()
    {
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget <= senseRadius * 1.2f && currentTarget.currentHitpoints > 0)
            {
                EngageTarget(currentTarget);
                return;
            }
        }

        currentTarget = FindBestTarget();
        if (currentTarget != null)
        {
            EngageTarget(currentTarget);
        }
    }

    private void EngageTarget(Unit target)
    {
        if (target == null || target.currentHitpoints <= 0 || target.ownerId == self.ownerId)
            return;

        currentTarget = target;

        if (brain.GetAttackTarget() != target)
            brain.SetCommand(new AttackCommand(target));
    }

    private Unit FindBestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, senseRadius, unitLayer);
        Unit bestTarget = null;
        float bestScore = float.NegativeInfinity;
        foreach (var hit in hits)
        {
            var unit = hit.GetComponent<Unit>();
            if (unit == null || unit == self || unit.currentHitpoints <= 0 || unit.ownerId == self.ownerId)
                continue;

            float score = ScoreTarget(unit);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = unit;
            }
        }
        return bestTarget;
    }

    private float ScoreTarget(Unit target)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        float distanceScore = 1f / (1f + distance);

        float hpScore = target.currentHitpoints / Mathf.Max(1f, target.maxHitpoints);
        float lowHpScore = 1f - hpScore;

        return distanceScore * 2.0f + lowHpScore * 1.5f;
    }

    public void TryAssistAgainst(Unit target)
    {
        if (target == null || target.currentHitpoints <= 0 || target.ownerId == self.ownerId)
            return;

        Unit activeTarget = brain.GetAttackTarget();
        if (activeTarget != null && activeTarget.currentHitpoints > 0)
            return;

        EngageTarget(target);
    }
}
