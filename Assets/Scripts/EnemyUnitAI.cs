using System.Runtime.CompilerServices;
using UnityEngine;

public class EnemyUnitAI : MonoBehaviour
{
    [SerializeField] private float senseRadius = 12f;
    [SerializeField] private float thinkInterval = 0.25f;
    [SerializeField] private LayerMask unitLayer;

    private Unit self;
    private UnitBrain brain;

    private Unit currentTarget;
    private float nextThinkTime;

    private void Awake()
    {
        self = GetComponent<Unit>();
        brain = GetComponent<UnitBrain>();
    }

    private void Update()
    {
        if (Time.time < nextThinkTime)
            return;
        nextThinkTime = Time.time + thinkInterval;
        Think();
    }

    private void Think()
    {
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            if (distanceToTarget <= senseRadius * 1.2f && currentTarget.currentHitpoints > 0)
            {
                brain.SetCommand(new AttackCommand(currentTarget));
                return;
            }
        }

        currentTarget = FindBestTarget();
        if (currentTarget != null)
        {
            brain.SetCommand(new AttackCommand(currentTarget));
        }
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
}
