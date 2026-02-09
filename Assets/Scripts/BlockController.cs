using UnityEngine;

public class BlockController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private bool isBlocking;
    [SerializeField] private float blockAngle = 90f;

    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int BlockStartHash = Animator.StringToHash("Block");

    public bool GetIsBlocking()
    {
        return isBlocking;
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        isBlocking = false;
    }

    public void SetBlocking(bool status)
    {
        if (isBlocking == status)
            return;
        isBlocking = status;
        if (!animator)
            return;

        // Controls whether we should remain in block state
        animator.SetBool(IsBlockingHash, isBlocking);

        // When turning block ON, play start once, then flow to loop
        if (isBlocking)
        {
            animator.ResetTrigger(BlockStartHash);
            animator.SetTrigger(BlockStartHash);
        }
        else
        {
            // Stop blocking, reset start trigger to avoid queued starts
            animator.ResetTrigger(BlockStartHash);
        }
    }

    public bool TryToBlock(Vector3 attackerPosition)
    {
        if (!isBlocking)
            return false;
        
        Vector3 directionToAttacker = (attackerPosition - transform.position);
        directionToAttacker.y = 0; // Ignore vertical difference
        if (directionToAttacker.sqrMagnitude < 0.0001f)
            return true; // Attacker is at the same position
        float angleToAttacker = Vector3.Angle(transform.forward, directionToAttacker.normalized);
        return angleToAttacker <= (blockAngle * 0.5f);
    }
}
