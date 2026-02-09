using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private Camera cam;

    public float panSpeed = 20f;
    public float panFastMultiplier = 2f;

    public float zoomSpeed = 100f;
    public float minHeight = 5f;
    public float maxHeight = 45f;
    private float targetHeight;

    public float rotationSpeed = 100f;

    public bool clampToBounds = false;
    public Vector2 boundsX = new Vector2(-50f, 50f);
    public Vector2 boundsZ = new Vector2(-50f, 50f);

    private InputAction move;
    private InputAction rotate;
    private InputAction zoom;
    private InputAction fastPan;

    private Transform cameraTransform;

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;

        cameraTransform = cam.transform;

        if (!pivot)
        {
            pivot = transform;
        }
        move = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: true);
        rotate = InputSystem.actions.FindAction("Player/RotateCamera", throwIfNotFound: true);
        zoom = InputSystem.actions.FindAction("Player/Zoom", throwIfNotFound: true);
        fastPan = InputSystem.actions.FindAction("Player/Sprint", throwIfNotFound: false);
    }

    void OnEnable()
    {
        move.Enable();
        rotate.Enable();
        zoom.Enable();
        if (fastPan != null)
            fastPan.Enable();
    }

    void OnDisable()
    {
        move.Disable();
        rotate.Disable();
        zoom.Disable();
        if (fastPan != null)
            fastPan.Disable();
    }

    private void Start()
    {
        targetHeight = cameraTransform.position.y;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        HandlePan(dt);
        HandleRotate(dt);
        HandleZoom(dt);

        if (clampToBounds)
        {
            Vector3 p = pivot.position;
            p.x = Mathf.Clamp(p.x, boundsX.x, boundsX.y);
            p.z = Mathf.Clamp(p.z, boundsZ.x, boundsZ.y);
            pivot.position = p;
            transform.position = p;
        }
    }

    private void HandlePan(float dt)
    {
        Vector2 input = move.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.0001f)
            return;

        float speed = panSpeed;
        if (fastPan != null && fastPan.IsPressed())
            speed *= panFastMultiplier;

        Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = transform.right; right.y = 0f; right.Normalize();

        Vector3 delta = (right * input.x + forward * input.y) * speed * dt;

        pivot.position += delta;
        transform.position = pivot.position;
    }

    private void HandleRotate(float dt)
    {
        float rotInput = rotate.ReadValue<float>();
        if (Mathf.Abs(rotInput) < 0.001f)
            return;

        float yaw = rotInput * rotationSpeed * dt;
        transform.RotateAround(pivot.position, Vector3.up, yaw);
    }

    private void HandleZoom(float dt)
    {
        float scroll = zoom.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float steps = scroll / 120f;
            targetHeight -= steps * zoomSpeed;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
        }

        Vector3 pos = cameraTransform.position;
        pos.y = Mathf.Lerp(pos.y, targetHeight, 12f * dt);
        cameraTransform.position = pos;
    }
}
