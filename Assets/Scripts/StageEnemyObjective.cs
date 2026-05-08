using UnityEngine;

public class StageEnemyObjective : MonoBehaviour
{
    [SerializeField] private int enemyFactionId = FactionRelations.EnemyFactionId;
    [SerializeField] private int playerFactionId = FactionRelations.Player1FactionId;
    [SerializeField] private bool countAllHostileFactions = false;
    [SerializeField] private StageExitTrigger exitToUnlock;
    [SerializeField] private int stageUnlockedOnComplete = 2;
    [SerializeField] private int goldReward = 0;

    public bool IsComplete { get; private set; }

    private void Awake()
    {
        playerFactionId = FactionRelations.UpgradeLegacyPlayerFactionId(playerFactionId);
        enemyFactionId = FactionRelations.UpgradeLegacyEnemyFactionId(enemyFactionId);
    }

    private void Start()
    {
        if (exitToUnlock != null)
            exitToUnlock.SetObjectiveComplete(false);
    }

    private void Update()
    {
        if (IsComplete)
            return;

        if (CountLivingEnemies() == 0)
            CompleteObjective();
    }

    private int CountLivingEnemies()
    {
        int count = 0;
        Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);

        foreach (Unit unit in units)
        {
            if (unit == null || unit.currentHitpoints <= 0f)
                continue;

            bool countsForObjective = countAllHostileFactions
                ? FactionRelations.AreHostile(playerFactionId, unit.ownerId)
                : unit.ownerId == enemyFactionId;

            if (countsForObjective)
                count++;
        }

        return count;
    }

    private void CompleteObjective()
    {
        IsComplete = true;

        GameProgress progress = GameProgress.GetOrCreate();
        progress.UnlockStage(stageUnlockedOnComplete);
        progress.AddGold(goldReward);

        if (exitToUnlock != null)
            exitToUnlock.SetObjectiveComplete(true);

        Debug.Log($"Stage objective complete. Unlocked stage {stageUnlockedOnComplete}.");
    }
}
