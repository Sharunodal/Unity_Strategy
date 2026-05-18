using System.Collections.Generic;
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
    [SerializeField] private RectTransform selectionBox;
    [SerializeField] private float selectionDragThreshold = 8f;

    private int localPlayerId = FactionRelations.Player1FactionId;
    private InputAction toggleRun;
    private InputAction toggleBlock;
    private InputAction toggleAutoCombo;
    private InputAction additiveSelect;
    private InputAction stopAction;
    private InputAction equipSword;
    private InputAction equipBow;
    private Vector2 selectionStartScreenPosition;
    private bool selectionPointerDown;
    private bool draggingSelectionBox;

    private void Awake()
    {
        if (!cam)
            cam = Camera.main;
        HideSelectionBox();
        toggleRun = InputSystem.actions.FindAction("Player/ToggleRun", true);
        toggleBlock = InputSystem.actions.FindAction("Player/ToggleBlock", true);
        toggleAutoCombo = InputSystem.actions.FindAction("Player/ToggleAutoCombo", false);
        additiveSelect = InputSystem.actions.FindAction("Player/AdditiveSelect", true);
        stopAction = InputSystem.actions.FindAction("Player/Stop", true);
        equipSword = InputSystem.actions.FindAction("Player/EquipSword", true);
        equipBow = InputSystem.actions.FindAction("Player/EquipBow", true);
    }

    private void OnEnable()
    {
        toggleRun.Enable();
        toggleBlock.Enable();
        toggleAutoCombo?.Enable();
        stopAction.Enable();
        equipSword.Enable();
        equipBow.Enable();
        toggleRun.performed += OnToggleRun;
        toggleBlock.performed += OnToggleBlock;
        if (toggleAutoCombo != null)
            toggleAutoCombo.performed += OnToggleAutoCombo;
        stopAction.performed += OnStop;
        equipSword.performed += OnEquipSword;
        equipBow.performed += OnEquipBow;
    }

    private void OnDisable()
    {
        equipBow.performed -= OnEquipBow;
        equipSword.performed -= OnEquipSword;
        stopAction.performed -= OnStop;
        if (toggleAutoCombo != null)
            toggleAutoCombo.performed -= OnToggleAutoCombo;
        toggleBlock.performed -= OnToggleBlock;
        toggleRun.performed -= OnToggleRun;
        equipBow.Disable();
        equipSword.Disable();
        stopAction.Disable();
        toggleAutoCombo?.Disable();
        toggleBlock.Disable();
        toggleRun.Disable();
        selectionPointerDown = false;
        draggingSelectionBox = false;
        HideSelectionBox();
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

    private void OnToggleAutoCombo(InputAction.CallbackContext context)
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
                current = brain.GetAutoComboToggled();
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
            brain.SetAutoComboToggled(newValue);
            Debug.Log($"Set auto combo toggled to {newValue} for unit {u.unitName}");
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
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            brain.SetWeapon(WeaponType.Sword);
        }
    }

    private void OnEquipBow(InputAction.CallbackContext context)
    {
        foreach (var s in selection.Selected)
        {
            var u = s.GetComponent<Unit>();
            var brain = s.GetComponent<UnitBrain>();
            if (u == null || brain == null)
                continue;
            if (u.ownerId != localPlayerId)
                continue;
            brain.SetWeapon(WeaponType.Bow);
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        HandleSelection();
        HandleOrders();
    }

    void HandleSelection()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUi())
                return;

            selectionPointerDown = true;
            draggingSelectionBox = false;
            selectionStartScreenPosition = Mouse.current.position.ReadValue();
            HideSelectionBox();
            return;
        }

        if (!selectionPointerDown)
            return;

        Vector2 currentScreenPosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.isPressed)
        {
            if (!draggingSelectionBox && Vector2.Distance(selectionStartScreenPosition, currentScreenPosition) >= selectionDragThreshold)
            {
                draggingSelectionBox = true;
                ShowSelectionBox();
            }

            if (draggingSelectionBox)
                UpdateSelectionBox(selectionStartScreenPosition, currentScreenPosition);

            return;
        }

        selectionPointerDown = false;

        if (draggingSelectionBox)
        {
            draggingSelectionBox = false;
            HideSelectionBox();
            SelectUnitsInScreenRectangle(selectionStartScreenPosition, currentScreenPosition);
            return;
        }

        SelectSingleUnitAt(currentScreenPosition);
    }

    private void SelectSingleUnitAt(Vector2 screenPosition)
    {
        bool additive = additiveSelect != null && additiveSelect.IsPressed();
        Ray ray = cam.ScreenPointToRay(screenPosition);
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

    private void SelectUnitsInScreenRectangle(Vector2 startScreenPosition, Vector2 endScreenPosition)
    {
        bool additive = additiveSelect != null && additiveSelect.IsPressed();
        Rect selectionRect = GetScreenRect(startScreenPosition, endScreenPosition);
        Selectable[] selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Exclude);
        List<Selectable> selectablesInRect = new();

        foreach (Selectable selectable in selectables)
        {
            if (selectable == null || !selectable.IsOwnedBy(localPlayerId))
                continue;

            Vector3 screenPosition = cam.WorldToScreenPoint(selectable.transform.position);
            if (screenPosition.z < 0f)
                continue;

            if (selectionRect.Contains(new Vector2(screenPosition.x, screenPosition.y)))
                selectablesInRect.Add(selectable);
        }

        selection.SelectMultiple(selectablesInRect, additive);
    }

    private Rect GetScreenRect(Vector2 startScreenPosition, Vector2 endScreenPosition)
    {
        Vector2 min = Vector2.Min(startScreenPosition, endScreenPosition);
        Vector2 max = Vector2.Max(startScreenPosition, endScreenPosition);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void ShowSelectionBox()
    {
        if (!selectionBox)
            return;

        selectionBox.gameObject.SetActive(true);
    }

    private void HideSelectionBox()
    {
        if (!selectionBox)
            return;

        selectionBox.gameObject.SetActive(false);
        selectionBox.sizeDelta = Vector2.zero;
    }

    private void UpdateSelectionBox(Vector2 startScreenPosition, Vector2 endScreenPosition)
    {
        if (!selectionBox)
            return;

        RectTransform parentRect = selectionBox.parent as RectTransform;
        if (!parentRect)
            return;

        selectionBox.anchorMin = new Vector2(0.5f, 0.5f);
        selectionBox.anchorMax = new Vector2(0.5f, 0.5f);
        selectionBox.pivot = new Vector2(0.5f, 0.5f);

        Camera uiCamera = GetSelectionBoxUiCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, startScreenPosition, uiCamera, out Vector2 startLocalPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, endScreenPosition, uiCamera, out Vector2 endLocalPosition);

        selectionBox.anchoredPosition = (startLocalPosition + endLocalPosition) * 0.5f;
        selectionBox.sizeDelta = new Vector2(
            Mathf.Abs(startLocalPosition.x - endLocalPosition.x),
            Mathf.Abs(startLocalPosition.y - endLocalPosition.y));
    }

    private Camera GetSelectionBoxUiCamera()
    {
        Canvas canvas = selectionBox.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera != null ? canvas.worldCamera : cam;

        return null;
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void HandleOrders()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;
        if (IsPointerOverUi())
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
