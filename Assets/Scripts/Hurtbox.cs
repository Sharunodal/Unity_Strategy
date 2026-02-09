using UnityEngine;

public enum BodyPart { Head, Chest, Torso, Arm, Leg }

public class Hurtbox : MonoBehaviour
{
    public Unit owner;
    public BodyPart part;
    public float damageMultiplier = 1f;
    private Collider cachedCollider;

    private void Awake()
    {
        if (!owner)
            owner = GetComponentInParent<Unit>();
        cachedCollider = GetComponent<Collider>();
    }

    public Vector3 GetAimWorldPosition()
    {
        if (cachedCollider != null)
            return cachedCollider.bounds.center;

        // Last resort fallback
        return transform.position;
    }
}
