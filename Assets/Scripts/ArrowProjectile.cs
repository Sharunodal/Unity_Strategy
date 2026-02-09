using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifeSeconds = 12f;

    [SerializeField] private bool stickOnHit = true;
    [SerializeField] private float stickDepth = 0.08f;
    [SerializeField] private bool alignToVelocityOnFlight = true;

    private Rigidbody rb;
    private Unit owner;
    private bool hasHit;

    public void Init(Unit ownerUnit, float dmg)
    {
        owner = ownerUnit;
        damage = dmg;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifeSeconds);
    }

    private void FixedUpdate()
    {
        if (!hasHit && alignToVelocityOnFlight && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.up, rb.linearVelocity.normalized);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            Debug.Log("Arrow already hit something, ignoring further collisions");
            return;
        }

        var collider = collision.collider;

        // Ignore hitting the self
        if (owner != null && collider.transform.IsChildOf(owner.transform))
            return;
   
        hasHit = true;

        Hurtbox hurtbox = collider.GetComponent<Hurtbox>();
        Unit target = hurtbox != null ? hurtbox.owner : collider.GetComponentInParent<Unit>();

        // Block check 
        bool blocked = false;
        if (target != null)
        {
            var block = target.GetComponent<BlockController>();
            if (block != null)
            {
                blocked = block.TryToBlock(owner != null ? owner.transform.position : transform.position);
                if (blocked)
                {
                    Debug.Log($"{target.unitName} blocked the arrow!");
                }
            }
        }

        if (target != null && !blocked)
        {
            float finalDamage = damage;
            if (hurtbox != null)
                finalDamage *= hurtbox.damageMultiplier;
            target.TakeDamage(finalDamage, owner);
            Debug.Log($"{target.unitName} got hit in {hurtbox.part} for {finalDamage} damage!");
        }

        Stick(collision);
    }

    private void Stick(Collision collision)
    {
        if (!stickOnHit)
        {
            Destroy(gameObject);
            return;
        }

        // Stop physics
        rb.isKinematic = true;
        rb.detectCollisions = false;
        rb.interpolation = RigidbodyInterpolation.None;

        // Choose the exact hurtbox part that was hit
        Transform parent = collision.collider.transform;

        // Compute desired world pose
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;

        if (collision.contactCount > 0)
        {
            var c = collision.GetContact(0);
            worldPos = c.point - c.normal * stickDepth;
        }

        // Cache the arrow's world scale before parenting to avoid scale issues
        Vector3 desiredWorldScale = transform.lossyScale;

        // Parent while keeping world pose
        transform.SetParent(parent, true);
        transform.position = worldPos;
        transform.rotation = worldRot;

        // Compensate scaling so world scale remains constant even under scaled bones/hurtboxes
        Vector3 parentWorldScale = parent.lossyScale;
        transform.localScale = new Vector3(
            parentWorldScale.x != 0f ? desiredWorldScale.x / parentWorldScale.x : 1f,
            parentWorldScale.y != 0f ? desiredWorldScale.y / parentWorldScale.y : 1f,
            parentWorldScale.z != 0f ? desiredWorldScale.z / parentWorldScale.z : 1f
        );

        // Disable collider after sticking
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }
}
