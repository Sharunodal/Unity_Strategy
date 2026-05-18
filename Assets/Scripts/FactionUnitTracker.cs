using System;
using System.Collections.Generic;
using UnityEngine;

public class FactionUnitTracker : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool showGameOverWhenPlayerDefeated = true;
    [SerializeField] private bool unlockExitWhenEnemiesDefeated = true;
    [SerializeField] private bool countAllHostileFactionsForStageClear = false;
    [SerializeField] private StageExitTrigger exitToUnlock;
    [SerializeField] private int stageUnlockedOnClear = 2;

    private int playerFactionId = FactionRelations.Player1FactionId;
    private int enemyFactionId = FactionRelations.EnemyFactionId;

    public event Action<int> FactionDefeated;
    public event Action<int, int> FactionUnitCountChanged;

    private readonly Dictionary<int, int> livingUnitCounts = new();
    private readonly Dictionary<Unit, int> registeredUnits = new();
    private bool gameOverTriggered;
    private bool hasSeenPlayerUnits;
    private bool initialUnitRegistrationComplete;
    private bool stageClearTriggered;

    private void Awake()
    {
        playerFactionId = FactionRelations.UpgradeLegacyPlayerFactionId(playerFactionId);
        enemyFactionId = FactionRelations.UpgradeLegacyEnemyFactionId(enemyFactionId);
    }

    private void Start()
    {
        if (unlockExitWhenEnemiesDefeated && exitToUnlock != null)
            exitToUnlock.SetObjectiveComplete(false);

        initialUnitRegistrationComplete = true;
    }

    public void RegisterUnit(Unit unit)
    {
        if (unit == null || unit.currentHitpoints <= 0f || registeredUnits.ContainsKey(unit))
            return;

        int ownerId = unit.ownerId;
        registeredUnits.Add(unit, ownerId);
        AddUnitToFaction(ownerId);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null || !registeredUnits.TryGetValue(unit, out int ownerId))
            return;

        registeredUnits.Remove(unit);
        RemoveUnitFromFaction(ownerId);
    }

    public void ChangeUnitFaction(Unit unit, int newOwnerId)
    {
        if (unit == null)
            return;

        if (!registeredUnits.TryGetValue(unit, out int oldOwnerId))
            return;

        if (oldOwnerId == newOwnerId)
            return;

        registeredUnits[unit] = newOwnerId;
        RemoveUnitFromFaction(oldOwnerId);
        AddUnitToFaction(newOwnerId);
    }

    public int GetLivingUnitCount(int factionId)
    {
        return livingUnitCounts.TryGetValue(factionId, out int count) ? count : 0;
    }

    public bool HasLivingUnits(int factionId)
    {
        return GetLivingUnitCount(factionId) > 0;
    }

    public int GetLivingHostileUnitCount(int factionId)
    {
        int count = 0;

        foreach (KeyValuePair<int, int> factionCount in livingUnitCounts)
        {
            if (FactionRelations.AreHostile(factionId, factionCount.Key))
                count += factionCount.Value;
        }

        return count;
    }

    private void AddUnitToFaction(int factionId)
    {
        int newCount = GetLivingUnitCount(factionId) + 1;
        livingUnitCounts[factionId] = newCount;
        FactionUnitCountChanged?.Invoke(factionId, newCount);
        CheckPlayerDefeat(factionId);
        CheckStageClearObjective();
    }

    private void RemoveUnitFromFaction(int factionId)
    {
        int newCount = Mathf.Max(0, GetLivingUnitCount(factionId) - 1);

        if (newCount == 0)
            livingUnitCounts.Remove(factionId);
        else
            livingUnitCounts[factionId] = newCount;

        FactionUnitCountChanged?.Invoke(factionId, newCount);
        CheckPlayerDefeat(factionId);
        CheckStageClearObjective();

        if (newCount == 0)
            FactionDefeated?.Invoke(factionId);
    }

    private void CheckPlayerDefeat(int changedFactionId)
    {
        if (changedFactionId == playerFactionId)
            CheckPlayerDefeat();
    }

    private void CheckPlayerDefeat()
    {
        if (!showGameOverWhenPlayerDefeated || gameOverTriggered || gameManager == null)
            return;

        int livingPlayerUnits = GetLivingUnitCount(playerFactionId);
        if (livingPlayerUnits > 0)
        {
            hasSeenPlayerUnits = true;
            return;
        }

        if (!hasSeenPlayerUnits)
            return;

        gameOverTriggered = true;
        gameManager.ShowGameOver();
    }

    private void CheckStageClearObjective()
    {
        if (!unlockExitWhenEnemiesDefeated || !initialUnitRegistrationComplete || stageClearTriggered)
            return;

        int livingEnemies = countAllHostileFactionsForStageClear
            ? GetLivingHostileUnitCount(playerFactionId)
            : GetLivingUnitCount(enemyFactionId);

        if (livingEnemies > 0)
            return;

        stageClearTriggered = true;
        GameProgress.GetOrCreate().UnlockStage(stageUnlockedOnClear);

        if (exitToUnlock != null)
            exitToUnlock.SetObjectiveComplete(true);

        Debug.Log($"Stage objective complete. Unlocked stage {stageUnlockedOnClear}.");
    }
}
