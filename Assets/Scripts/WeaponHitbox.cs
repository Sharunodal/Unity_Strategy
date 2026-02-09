using System.Collections.Generic;
using UnityEngine;

public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private Unit owner;
    [SerializeField] private float damage = 10f;
    [SerializeField] private BoxCollider hitbox;
    [SerializeField] private LayerMask unitLayer;

    private bool active;
    private readonly HashSet<Unit> hitThisSwing = new();
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<Unit>();
    }

    public void BeginAttack()
    {
        active = true;
        hitThisSwing.Clear();
    }

    public void EndAttack()
    {
        active = false;
    }

    // Check for overlaps with the hitbox instead of trigger or collisions for better control
    private void Update()
    {
        if (!active || hitbox == null)
            return;

        // BoxCollider is defined in its local space
        Vector3 centerWS = hitbox.transform.TransformPoint(hitbox.center);

        // Convert local size -> world half extents
        Vector3 lossy = hitbox.transform.lossyScale;
        Vector3 halfExtentsWS = Vector3.Scale(hitbox.size * 0.5f, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));

        Quaternion rotWS = hitbox.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(centerWS, halfExtentsWS, hits, rotWS, unitLayer);

        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (!col) continue;

            Unit target = col.transform.GetComponentInParent<Unit>();
            if (target == null || target == owner)
                continue;
            if (hitThisSwing.Contains(target))
                continue;

            hitThisSwing.Add(target);

            if (owner != null && target.ownerId == owner.ownerId)
                continue;

            var block = target.GetComponent<BlockController>();
            bool blocked = block != null && block.TryToBlock(owner != null ? owner.transform.position : transform.position);
            if (blocked)
                continue;

            target.TakeDamage(damage, owner);
            Debug.Log($"{owner.unitName} hit {target.unitName} for {damage} damage!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitbox == null) return;

        Vector3 centerWS = hitbox.transform.TransformPoint(hitbox.center);
        Vector3 lossy = hitbox.transform.lossyScale;
        Vector3 halfExtentsWS = Vector3.Scale(hitbox.size * 0.5f, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
        Quaternion rotWS = hitbox.transform.rotation;

        Gizmos.matrix = Matrix4x4.TRS(centerWS, rotWS, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtentsWS * 2f);
    }
}
