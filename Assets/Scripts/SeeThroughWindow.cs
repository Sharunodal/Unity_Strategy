using UnityEngine;

public class SeeThroughWindow : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private BuildingInteriorTrigger interiorTrigger;
    [SerializeField] private Renderer[] occluders;

    [SerializeField] private bool requiredLineOfSightBlock = true;
    [SerializeField] private LayerMask occluderMask = ~0;

    [SerializeField] private float holeRadius = 0.15f;
    [SerializeField] private float holeSoftness = 0.05f;

    private MaterialPropertyBlock mpb;

    static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    static readonly int HoleRadiusID = Shader.PropertyToID("_HoleRadius");
    static readonly int HoleSoftnessID = Shader.PropertyToID("_HoleSoftness");

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;
        mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (!cam || !gameManager || !selectionManager || !interiorTrigger)
        {
            DisableWindow();
            return;
        }

        Unit observedUnit = gameManager.ObservedUnit;
        if (!observedUnit)
        {
            DisableWindow();
            return;
        }

        Selectable selectable = observedUnit.GetComponent<Selectable>();
        bool isSelected = selectable && selectionManager.IsSelected(selectable);
        if (!isSelected)
        {
            DisableWindow();
            return;
        }

        if (!interiorTrigger.IsInside(observedUnit))
        {
            DisableWindow();
            return;
        }

        Vector3 worldTarget = observedUnit.transform.position;
        Vector3 viewportPos = cam.WorldToViewportPoint(worldTarget);
        if (viewportPos.z <= 0f)
        {
            DisableWindow();
            return;
        }

        bool blocked = true;
        if (requiredLineOfSightBlock)
        {
            Vector3 origin = cam.transform.position;
            Vector3 direction = worldTarget - origin;
            float distance = direction.magnitude;

            blocked = Physics.Raycast(origin, direction.normalized, distance, occluderMask);
        }
        if (!blocked)
        {
            DisableWindow();
            return;
        }

        Vector2 holeCenter = new(viewportPos.x, viewportPos.y);
        ApplyWindow(holeCenter, holeRadius, holeSoftness);
    }

    private void ApplyWindow(Vector2 holeCenter, float radius, float softness)
    {
        foreach (Renderer rend in occluders)
        {
            if (!rend)
                continue;

            rend.GetPropertyBlock(mpb);
            mpb.SetVector(HoleCenterID, holeCenter);
            mpb.SetFloat(HoleRadiusID, radius);
            mpb.SetFloat(HoleSoftnessID, softness);
            rend.SetPropertyBlock(mpb);
        }
    }

    private void DisableWindow()
    {
        foreach (Renderer rend in occluders)
        {
            if (!rend)
                continue;

            rend.GetPropertyBlock(mpb);
            mpb.SetFloat(HoleRadiusID, 0f);
            mpb.SetFloat(HoleSoftnessID, 0f);
            rend.SetPropertyBlock(mpb);
        }
    }
}
