using UnityEngine;

public class SeeThroughWindow : MonoBehaviour
{
    public Camera cam;
    public Transform target;

    public Renderer[] occluders;

    public float holeRadius = 0.15f;
    public float holeSoftness = 0.05f;

    MaterialPropertyBlock mpb;

    static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    static readonly int HoleRadiusID = Shader.PropertyToID("_HoleRadius");
    static readonly int HoleSoftnessID = Shader.PropertyToID("_HoleSoftness");

    void Awake()
    {
        if (!cam)
            cam = Camera.main;

        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (!target)
            return;

        Vector3 viewportPos = cam.WorldToViewportPoint(target.position);

        Vector2 holeCenter = new Vector2(viewportPos.x, viewportPos.y);

        foreach (Renderer r in occluders)
        {
            r.GetPropertyBlock(mpb);

            mpb.SetVector(HoleCenterID, holeCenter);
            mpb.SetFloat(HoleRadiusID, holeRadius);
            mpb.SetFloat(HoleSoftnessID, holeSoftness);

            r.SetPropertyBlock(mpb);
        }
    }
}
