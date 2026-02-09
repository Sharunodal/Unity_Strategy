using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask unitLayer;

    [SerializeField] private Camera cam;
    [SerializeField] private SelectionManager selection;
    [SerializeField] private CommandSystem commands;

    [SerializeField] private int localPlayerId = 0;

    private InputAction toggleRun;
    private InputAction toggleBlock;
    private InputAction additiveSelect;
    private InputAction stopAction;
    private InputAction equipSword;
    private InputAction equipBow;

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;
        toggleRun = InputSystem.actions.FindAction("Player/ToggleRun", true);
        toggleBlock = InputSystem.actions.FindAction("Player/ToggleBlock", true);
        additiveSelect = InputSystem.actions.FindAction("Player/AdditiveSelect", true);
        stopAction = InputSystem.actions.FindAction("Player/Stop", true);
        equipSword = InputSystem.actions.FindAction("Player/EquipSword", true);
        equipBow = InputSystem.actions.FindAction("Player/EquipBow", true);
    }

    private void OnEnable()
    {
        toggleRun.Enable();
        toggleBlock.Enable();
        stopAction.Enable();
        equipSword.Enable();
        equipBow.Enable();
        toggleRun.performed += OnToggleRun;
        toggleBlock.performed += OnToggleBlock;
        stopAction.performed += OnStop;
        equipSword.performed += OnEquipSword;
        equipBow.performed += OnEquipBow;
    }

    private void OnDisable()
    {
        equipBow.performed -= OnEquipBow;
        equipSword.performed -= OnEquipSword;
        stopAction.performed -= OnStop;
        toggleBlock.performed -= OnToggleBlock;
        toggleRun.performed -= OnToggleRun;
        equipBow.Disable();
        equipSword.Disable();
        stopAction.Disable();
        toggleBlock.Disable();
        toggleRun.Disable();
    }

    private void OnToggleRun(InputAction.CallbackContext context)
    {
        // Toggle based on first selected unit
        bool? current = null;
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            if (current == null)
                current = brain.GetRunToggled();
        }

        if (current == null)
            return;
        bool newValue = !current.Value;
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            brain.SetRunToggled(newValue);
        }
    }

    void OnToggleBlock(InputAction.CallbackContext context)
    {
        bool? current = null;
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            if (current == null)
                current = brain.GetBlockToggled();
        }

        if (current == null)
            return;
        bool newValue = !current.Value;
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            brain.SetBlockToggled(newValue);
            Debug.Log($"Set block toggled to {newValue} for unit {u.unitName}");
        }
    }

    private void OnStop(InputAction.CallbackContext context)
    {
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            brain.StopAll();
        }
    }

    private void OnEquipSword(InputAction.CallbackContext context)
    {
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            if (u == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            u.EquipWeapon(WeaponType.Sword);
        }
    }

    private void OnEquipBow(InputAction.CallbackContext context)
    {
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            if (u == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            u.EquipWeapon(WeaponType.Bow);
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
        HandleSelection();
        HandleOrders();
    }

    void HandleSelection()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;
        bool additive = additiveSelect != null && additiveSelect.IsPressed();
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 500f, unitLayer))
        {
            var selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsOwnedBy(localPlayerId))
                selection.SelectSingle(selectable, additive);
            else if (!additive)
                selection.ClearSelection();
        }
        else if (!additive)
        {
            selection.ClearSelection();
        }
    }

    void HandleOrders()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hitUnit, 500f, unitLayer))
        {
            var target = hitUnit.collider.GetComponentInParent<Unit>();
            if (target != null)
            {
                commands.IssueFollowOrAttackCommand(selection, target, localPlayerId);
                return;
            }
        }
        if (Physics.Raycast(ray, out var hitGround, 500f, groundLayer))
        {
            commands.IssueMoveCommand(selection, hitGround.point);
        }
    }
}
