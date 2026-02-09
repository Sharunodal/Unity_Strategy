using UnityEngine;

public class BowWeapon : MonoBehaviour
{
    [SerializeField] private Transform shotStartLocation;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpeed = 40f;

    [SerializeField] private float spreadDegreesAt10m = 1f;
    [SerializeField] private float distanceForSpread = 10f;
    [SerializeField] private float maxSpreadDegrees = 2f;
    [SerializeField] private float verticalBias = 0.5f;

    [SerializeField] private BodyPart aimPart = BodyPart.Chest;
    private Hurtbox[] cachedHurtboxes;

    private bool highArc = false;

    private Unit currentTarget;

    public void SetTarget(Unit target)
    {
        currentTarget = target;
        cachedHurtboxes = target != null ? target.GetComponentsInChildren<Hurtbox>() : null;
    }

    public void SetHighArc(bool enabled)
    {
        highArc = enabled;
    }

    public void SetAimPart(BodyPart part)
    {
        aimPart = part;
    }

    private Hurtbox ChooseAimHurtbox()
    {
        if (currentTarget == null) return null;

        if (cachedHurtboxes == null || cachedHurtboxes.Length == 0)
            return null;

        // Find requested part
        foreach (var hb in cachedHurtboxes)
            if (hb != null && hb.part == aimPart)
                return hb;

        // Fallback to chest
        foreach (var hb in cachedHurtboxes)
            if (hb != null && hb.part == BodyPart.Chest)
                return hb;

        return null;
    }

    public void FireArrow()
    {
        if (!shotStartLocation || !arrowPrefab || currentTarget == null) return;

        Vector3 start = shotStartLocation.position;
        Hurtbox aimTarget = ChooseAimHurtbox();
        Vector3 aimPoint = aimTarget != null ? aimTarget.GetAimWorldPosition()
               : currentTarget.transform.position + Vector3.up * 1.6f;

        // Aim to hit
        if (!TryGetBallisticVelocity(start, aimPoint, arrowSpeed, Physics.gravity.y, highArc, out Vector3 v0))
        {
            Vector3 dir = (aimPoint - start).normalized;
            v0 = (dir + Vector3.up * 0.25f).normalized * arrowSpeed;
        }

        // Miss chance based on distance, up to a max
        float distance = Vector3.Distance(start, aimPoint);
        float spread = spreadDegreesAt10m * (distance / Mathf.Max(0.01f, distanceForSpread));
        spread = Mathf.Min(spread, maxSpreadDegrees);

        v0 = ApplySpread(v0, spread, verticalBias);

        Vector3 flightDir = v0.normalized;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, flightDir);

        var go = Instantiate(arrowPrefab, start, rotation);

        if (go.TryGetComponent<ArrowProjectile>(out var proj))
            proj.Init(GetComponentInParent<Unit>(), GetComponentInParent<Unit>().attackDamage);

        if (go.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = v0;
    }

    private static Vector3 ApplySpread(Vector3 velocity, float spreadDegrees, float verticalBias)
    {
        if (spreadDegrees <= 0.0001f) return velocity;

        Vector3 dir = velocity.normalized;

        // Build a stable basis around the shot direction
        Vector3 right = Vector3.Cross(dir, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(dir, Vector3.forward);
        right.Normalize();

        Vector3 up = Vector3.Cross(right, dir).normalized;

        // Random yaw/pitch in degrees
        float yaw = Random.Range(-spreadDegrees, spreadDegrees);

        // Slightly more likely to shoot high than low
        float pitch = Random.Range(-spreadDegrees, spreadDegrees);
        pitch += Random.Range(0f, spreadDegrees) * Mathf.Clamp01(verticalBias);

        Quaternion rot = Quaternion.AngleAxis(yaw, up) * Quaternion.AngleAxis(pitch, right);
        Vector3 newDir = (rot * dir).normalized;

        return newDir * velocity.magnitude;
    }

    // Returns an initial velocity that will hit target with given speed
    private static bool TryGetBallisticVelocity(
        Vector3 start, Vector3 target, float speed, float gravityY, bool highArc, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        float g = -gravityY;
        Vector3 delta = target - start;
        Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);
        float x = deltaXZ.magnitude;
        float y = delta.y;

        if (x < 0.0001f)
            return false;

        float v2 = speed * speed;
        float v4 = v2 * v2;

        float disc = v4 - g * (g * x * x + 2f * y * v2);
        if (disc < 0f)
            return false;

        float sqrt = Mathf.Sqrt(disc);

        float tanTheta = (v2 + (highArc ? sqrt : -sqrt)) / (g * x);
        float cos = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
        float sin = tanTheta * cos;

        Vector3 dirXZ = deltaXZ / x;

        velocity = dirXZ * (speed * cos) + Vector3.up * (speed * sin);
        return true;
    }
}
