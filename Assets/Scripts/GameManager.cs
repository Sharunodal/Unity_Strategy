using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private GameObject unitStatsPanel;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private string firstSceneName = "Stage 1";
    [SerializeField] private TMPro.TextMeshProUGUI nameText;
    [SerializeField] private TMPro.TextMeshProUGUI hitpointsText;
    [SerializeField] private TMPro.TextMeshProUGUI staminaText;
    [SerializeField] private TMPro.TextMeshProUGUI speedText;
    [SerializeField] private TMPro.TextMeshProUGUI hungerText;

    private InputAction pauseAction;

    private Unit observedUnit;
    public Unit ObservedUnit => observedUnit;

    public bool isGameActive = true;
    public bool paused = false;
    public bool gameOver = false;

    private void Awake()
    {
        pauseAction = InputSystem.actions.FindAction("UI/Cancel");
    }

    void Start()
    {
        Time.timeScale = 1f;
        paused = false;
        isGameActive = true;
        gameOver = false;

        if (pauseScreen)
            pauseScreen.SetActive(false);
        if (gameOverScreen)
            gameOverScreen.SetActive(false);

        RefreshSelection();
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed;
        }

        if (selectionManager)
            selectionManager.SelectionChanged += RefreshSelection;
        RefreshSelection();
    }

    private void OnDisable()
    {
        if (selectionManager)
            selectionManager.SelectionChanged -= RefreshSelection;
        SetObservedUnit(null);

        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();
        }
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
            if (pauseScreen)
                pauseScreen.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            paused = false;
            if (pauseScreen)
                pauseScreen.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void ShowGameOver()
    {
        isGameActive = false;
        paused = false;
        gameOver = true;

        if (pauseScreen)
            pauseScreen.SetActive(false);
        if (gameOverScreen)
            gameOverScreen.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGameFromBeginning()
    {
        GameProgress.GetOrCreate().ResetProgress();
        LoadSceneFromNormalTime(firstSceneName);
    }

    public void RetryCurrentStage()
    {
        LoadSceneFromNormalTime(SceneManager.GetActiveScene().name);
    }

    private void LoadSceneFromNormalTime(string sceneName)
    {
        isGameActive = true;
        paused = false;
        Time.timeScale = 1f;

        if (pauseScreen)
            pauseScreen.SetActive(false);
        if (gameOverScreen)
            gameOverScreen.SetActive(false);

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        SceneManager.LoadScene(0);
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
