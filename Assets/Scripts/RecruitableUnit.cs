using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Unit))]
public class RecruitableUnit : MonoBehaviour
{
    [SerializeField] private int defaultRecruitingFactionId = FactionRelations.Player1FactionId;
    [SerializeField] private GameObject conversationPanel;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI conversationText;
    [SerializeField] private Button button1;
    [SerializeField] private TextMeshProUGUI button1Text;
    [SerializeField] private Button button2;
    [SerializeField] private TextMeshProUGUI button2Text;
    [SerializeField] private Button button3;
    [SerializeField] private TextMeshProUGUI button3Text;
    [SerializeField] private float conversationRange = 2.25f;
    [SerializeField] private Unit[] unitsToRecruit;
    [SerializeField] private int goldCost = 0;
    [SerializeField] private int goldRewardOnRecruit = 0;
    [SerializeField] private bool disableAfterRecruit = true;

    private Unit speakerUnit;
    private bool recruited;
    private bool hasOpenConversation;
    private int activeRecruitingFactionId;
    public float ConversationRange => conversationRange;

    private void Awake()
    {
        speakerUnit = GetComponent<Unit>();
        activeRecruitingFactionId = defaultRecruitingFactionId;
        HideConversationPanel();
    }

    public bool CanOpenForFaction(int factionId)
    {
        return !recruited
            && speakerUnit != null
            && speakerUnit.currentHitpoints > 0f
            && FactionRelations.AreFriendly(factionId, speakerUnit.ownerId)
            && !AreRecruitTargetsAlreadyOwnedBy(factionId);
    }

    public void OpenConversation()
    {
        OpenConversationForFaction(defaultRecruitingFactionId);
    }

    public void OpenConversationForFaction(int factionId)
    {
        if (!CanOpenForFaction(factionId))
            return;

        activeRecruitingFactionId = factionId;
        hasOpenConversation = true;
        ShowGreeting();
    }

    public void CloseConversation()
    {
        hasOpenConversation = false;
        HideConversationPanel();
    }

    public void RecruitFromConversation()
    {
        Recruit();
    }

    public bool Recruit()
    {
        int factionId = hasOpenConversation ? activeRecruitingFactionId : defaultRecruitingFactionId;
        return RecruitForFaction(factionId);
    }

    public bool RecruitForFaction(int factionId)
    {
        if (!CanOpenForFaction(factionId))
            return false;

        GameProgress progress = GameProgress.GetOrCreate();
        if (!progress.SpendGold(goldCost))
            return false;

        int recruitedCount = RecruitTargetsForFaction(factionId, progress);
        if (recruitedCount == 0)
            return false;

        recruited = true;
        progress.AddGold(goldRewardOnRecruit);
        ShowRecruited();

        if (disableAfterRecruit)
            enabled = false;

        Debug.Log($"Recruited {recruitedCount} unit(s) for faction {factionId}.");
        return true;
    }

    private void ShowGreeting()
    {
        ShowConversationPanel("Hello, do you have a minute?");
        ConfigureButton(button1, button1Text, "Greet", ShowRecruitPrompt);
        ConfigureButton(button2, button2Text, "Leave", CloseConversation);
        ConfigureButton(button3, button3Text, string.Empty, null);
    }

    private void ShowRecruitPrompt()
    {
        SetConversationText("It's good to see a friendly face! Would you mind teaming up with us to try and find a safe place to camp?");
        ConfigureButton(button1, button1Text, "Recruit", RecruitFromConversation);
        ConfigureButton(button2, button2Text, "Leave", CloseConversation);
        ConfigureButton(button3, button3Text, string.Empty, null);
    }

    private void ShowRecruited()
    {
        SetConversationText("Thank you! These are dangerous times...");
        ConfigureButton(button1, button1Text, "Let's go!", CloseConversation);
        ConfigureButton(button2, button2Text, string.Empty, null);
        ConfigureButton(button3, button3Text, string.Empty, null);
    }

    private void ShowConversationPanel(string message)
    {
        if (conversationPanel != null)
            conversationPanel.SetActive(true);

        if (unitNameText != null)
            unitNameText.text = speakerUnit != null ? speakerUnit.unitName : string.Empty;

        SetConversationText(message);
    }

    private void HideConversationPanel()
    {
        ClearButtons();

        if (conversationPanel != null)
            conversationPanel.SetActive(false);
    }

    private void SetConversationText(string message)
    {
        if (conversationText != null)
            conversationText.text = message;
    }

    private void ClearButtons()
    {
        ConfigureButton(button1, button1Text, string.Empty, null);
        ConfigureButton(button2, button2Text, string.Empty, null);
        ConfigureButton(button3, button3Text, string.Empty, null);
    }

    private void ConfigureButton(Button button, TextMeshProUGUI label, string text, UnityAction action)
    {
        if (button == null)
            return;

        // Remove all listeners to prevent unintended behavior when reusing buttons for different prompts
        button.onClick.RemoveAllListeners();

        bool active = action != null;
        button.gameObject.SetActive(active);

        if (label != null)
            label.text = active ? text : string.Empty;

        if (active)
            button.onClick.AddListener(action);
    }

    private int RecruitTargetsForFaction(int factionId, GameProgress progress)
    {
        int recruitedCount = 0;

        if (unitsToRecruit != null)
        {
            foreach (Unit recruit in unitsToRecruit)
            {
                if (RecruitUnit(recruit, factionId, progress))
                    recruitedCount++;
            }
        }

        if (recruitedCount == 0 && RecruitUnit(speakerUnit, factionId, progress))
            recruitedCount++;

        return recruitedCount;
    }

    private bool RecruitUnit(Unit recruit, int factionId, GameProgress progress)
    {
        if (recruit == null || recruit.currentHitpoints <= 0f)
            return false;

        recruit.SetOwnerId(factionId);
        progress.RegisterOrUpdatePlayerUnit(recruit);

        EnemyUnitAI enemyAI = recruit.GetComponent<EnemyUnitAI>();
        if (enemyAI != null)
            enemyAI.enabled = false;

        RecruitableUnit recruitable = recruit.GetComponent<RecruitableUnit>();
        if (recruitable != null && recruitable != this)
            recruitable.MarkRecruitmentCompleted();

        return true;
    }

    private bool AreRecruitTargetsAlreadyOwnedBy(int factionId)
    {
        bool hasTarget = false;

        if (unitsToRecruit != null)
        {
            foreach (Unit recruit in unitsToRecruit)
            {
                if (recruit == null)
                    continue;

                hasTarget = true;
                if (recruit.ownerId != factionId)
                    return false;
            }
        }

        if (!hasTarget && speakerUnit != null)
            return speakerUnit.ownerId == factionId;

        return hasTarget;
    }

    private void MarkRecruitmentCompleted()
    {
        recruited = true;

        if (disableAfterRecruit)
            enabled = false;
    }
}
