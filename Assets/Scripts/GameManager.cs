using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private GameObject unitStatsPanel;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI hitpointsText;
    [SerializeField] private TMPro.TextMeshProUGUI staminaText;
    [SerializeField] private TMPro.TextMeshProUGUI speedText;
    [SerializeField] private TMPro.TextMeshProUGUI hungerText;

    private InputAction pauseAction;

    private Unit observedUnit;

    public bool isGameActive = true;
    public bool paused = false;

    private void Awake()
    {
        pauseAction = InputSystem.actions.FindAction("UI/Cancel");
    }

    void Start()
    {
        RefreshSelection();
    }

    private void OnEnable()
    {
        pauseAction.Enable();
        pauseAction.performed += OnPausePerformed;

        selectionManager.SelectionChanged += RefreshSelection;
        RefreshSelection();
    }

    private void OnDisable()
    {
        if (selectionManager)
            selectionManager.SelectionChanged -= RefreshSelection;
        SetObservedUnit(null);

        pauseAction.performed -= OnPausePerformed;
        pauseAction.Disable();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (isGameActive)
        {
            ChangePaused();
        }
    }

    public void ChangePaused()
    {
        if (!paused)
        {
            paused = true;
            pauseScreen.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            paused = false;
            pauseScreen.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private void SetObservedUnit(Unit unit)
    {
        // Use Unity-null check (important for destroyed objects)
        if (observedUnit)
            observedUnit.statsChanged -= OnObservedUnitStatsChanged;

        observedUnit = unit;

        if (observedUnit)
            observedUnit.statsChanged += OnObservedUnitStatsChanged;
    }

    private void RefreshSelection()
    {
        if (!this || !unitStatsPanel || !selectionManager) return;

        // Find first valid selected unit (selection may contain destroyed entries)
        Unit unit = null;

        for (int i = 0; i < selectionManager.Selected.Count; i++)
        {
            var sel = selectionManager.Selected[i];
            if (!sel) continue;

            unit = sel.GetComponent<Unit>();
            if (unit) break;
        }

        SetObservedUnit(unit);

        unitStatsPanel.SetActive(observedUnit);

        if (observedUnit)
            RedrawStats(observedUnit);
    }

    private void OnObservedUnitStatsChanged()
    {
        if (!this || !unitStatsPanel || !observedUnit) return;
        RedrawStats(observedUnit);
    }

    private void RedrawStats(Unit unit)
    {
        nameText.text = unit.unitName;
        hitpointsText.text = $"HP:\n{unit.currentHitpoints:0}/{unit.maxHitpoints:0}";
        staminaText.text = $"Stamina:\n{unit.currentStamina:0}/{unit.maxStamina:0}";
        speedText.text = $"Speed:\n{unit.walkSpeed:0.0}";
        hungerText.text = $"Hunger:\n{unit.currentHunger:0}/{unit.maxHunger:0}";
    }
}
