using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum FactionAttitude
{
    Neutral,
    Friendly,
    Hostile
}

[Serializable]
public class FactionRelationship
{
    public int firstFactionId = FactionRelations.Player1FactionId;
    public int secondFactionId = FactionRelations.EnemyFactionId;
    public FactionAttitude attitude = FactionAttitude.Hostile;

    public FactionRelationship()
    {
    }

    public FactionRelationship(int firstFactionId, int secondFactionId, FactionAttitude attitude)
    {
        this.firstFactionId = firstFactionId;
        this.secondFactionId = secondFactionId;
        this.attitude = attitude;
    }

    public bool Matches(int factionA, int factionB)
    {
        return (firstFactionId == factionA && secondFactionId == factionB)
            || (firstFactionId == factionB && secondFactionId == factionA);
    }
}

public static class FactionRelations
{
    public const int LegacyPlayerFactionId = 0;

    public const int Player1FactionId = 1;
    public const int Player2FactionId = 2;
    public const int EnemyFactionId = 3;

    public static bool AreFriendly(int factionA, int factionB)
    {
        return GetAttitude(factionA, factionB) == FactionAttitude.Friendly;
    }

    public static bool AreHostile(int factionA, int factionB)
    {
        return GetAttitude(factionA, factionB) == FactionAttitude.Hostile;
    }

    public static int UpgradeLegacyPlayerFactionId(int factionId)
    {
        return factionId == LegacyPlayerFactionId ? Player1FactionId : factionId;
    }

    public static int UpgradeLegacyEnemyFactionId(int factionId)
    {
        return factionId == Player1FactionId ? EnemyFactionId : factionId;
    }

    public static FactionAttitude GetAttitude(int factionA, int factionB)
    {
        if (factionA == factionB)
            return FactionAttitude.Friendly;

        if (GameProgress.Instance != null)
            return GameProgress.Instance.GetFactionAttitude(factionA, factionB);

        return GetDefaultAttitude(factionA, factionB);
    }

    public static FactionAttitude GetDefaultAttitude(int factionA, int factionB)
    {
        if (factionA == factionB)
            return FactionAttitude.Friendly;

        bool factionAIsPlayer = factionA == Player1FactionId || factionA == Player2FactionId;
        bool factionBIsPlayer = factionB == Player1FactionId || factionB == Player2FactionId;

        if (factionAIsPlayer && factionBIsPlayer)
            return FactionAttitude.Friendly;

        if ((factionAIsPlayer && factionB == EnemyFactionId) || (factionBIsPlayer && factionA == EnemyFactionId))
            return FactionAttitude.Hostile;

        return FactionAttitude.Neutral;
    }
}

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    public int Gold { get; private set; }
    public int HighestUnlockedStage { get; private set; } = 1;

    [SerializeField] private List<FactionRelationship> factionRelationships = new()
    {
        new FactionRelationship(FactionRelations.Player1FactionId, FactionRelations.Player2FactionId, FactionAttitude.Friendly),
        new FactionRelationship(FactionRelations.Player1FactionId, FactionRelations.EnemyFactionId, FactionAttitude.Hostile),
        new FactionRelationship(FactionRelations.Player2FactionId, FactionRelations.EnemyFactionId, FactionAttitude.Hostile)
    };

    private readonly List<UnitSaveData> savedPlayerUnits = new();

    public IReadOnlyList<UnitSaveData> SavedPlayerUnits => savedPlayerUnits;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static GameProgress GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        GameObject progressObject = new GameObject(nameof(GameProgress));
        return progressObject.AddComponent<GameProgress>();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }

    public void UnlockStage(int stageNumber)
    {
        if (stageNumber > HighestUnlockedStage)
            HighestUnlockedStage = stageNumber;
    }

    public bool IsStageUnlocked(int stageNumber)
    {
        return stageNumber <= HighestUnlockedStage;
    }

    public FactionAttitude GetFactionAttitude(int factionA, int factionB)
    {
        if (factionA == factionB)
            return FactionAttitude.Friendly;

        foreach (FactionRelationship relationship in factionRelationships)
        {
            if (relationship != null && relationship.Matches(factionA, factionB))
                return relationship.attitude;
        }

        return FactionRelations.GetDefaultAttitude(factionA, factionB);
    }

    public void ResetProgress()
    {
        Gold = 0;
        HighestUnlockedStage = 1;
        savedPlayerUnits.Clear();
    }

    public void SavePlayerUnits(List<UnitSaveData> units)
    {
        savedPlayerUnits.Clear();
        savedPlayerUnits.AddRange(units);
    }

    public void SavePlayerUnitsFromScene(int playerFactionId)
    {
        Unit[] sceneUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
        List<UnitSaveData> unitsToSave = new();

        foreach (Unit unit in sceneUnits)
        {
            if (unit == null || unit.ownerId != playerFactionId || unit.currentHitpoints <= 0f)
                continue;

            unitsToSave.Add(unit.CreateSaveData());
        }

        SavePlayerUnits(unitsToSave);
    }

    public void ApplySavedUnitsToScene(int playerFactionId)
    {
        if (!HasSavedUnits())
            return;

        Unit[] sceneUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);
        HashSet<Unit> restoredUnits = new();

        foreach (UnitSaveData saveData in savedPlayerUnits)
        {
            Unit matchingUnit = FindMatchingSceneUnit(saveData, sceneUnits, restoredUnits);
            if (matchingUnit == null)
            {
                Debug.LogWarning($"No scene unit found for saved unit '{saveData.persistentId}'. Add a matching persistent id in this scene if this unit should appear here.");
                continue;
            }

            matchingUnit.ApplySaveData(saveData);
            matchingUnit.ownerId = playerFactionId;
            restoredUnits.Add(matchingUnit);
        }
    }

    public bool HasSavedUnits()
    {
        return savedPlayerUnits.Count > 0;
    }

    public void RegisterOrUpdatePlayerUnit(Unit unit)
    {
        if (unit == null)
            return;

        UnitSaveData newSaveData = unit.CreateSaveData();
        for (int i = 0; i < savedPlayerUnits.Count; i++)
        {
            if (savedPlayerUnits[i].persistentId == newSaveData.persistentId)
            {
                savedPlayerUnits[i] = newSaveData;
                return;
            }
        }

        savedPlayerUnits.Add(newSaveData);
    }

    private Unit FindMatchingSceneUnit(UnitSaveData saveData, Unit[] sceneUnits, HashSet<Unit> restoredUnits)
    {
        foreach (Unit unit in sceneUnits)
        {
            if (unit == null || restoredUnits.Contains(unit))
                continue;

            if (saveData.Matches(unit))
                return unit;
        }

        return null;
    }
}
