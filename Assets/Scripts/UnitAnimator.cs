using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class UnitAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private UnitBrain brain;
    private NavMeshAgent agent;
    private BowWeapon bowWeaponScript;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int BowShootHash = Animator.StringToHash("ShootBow");

    private void Awake()
    {
        brain = GetComponent<UnitBrain>();
        agent = GetComponent<NavMeshAgent>();
        bowWeaponScript = GetComponentInChildren<BowWeapon>(true);
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
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    private void OnAttackTriggered()
    {
        animator.SetTrigger(AttackHash);
    }

    private void OnRangedShot()
    {
        if (bowWeaponScript != null)
        {
            bowWeaponScript.SetTarget(brain.GetAttackTarget());
            animator.SetTrigger(BowShootHash);
        }
    }
}
