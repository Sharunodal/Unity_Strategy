using System;
using UnityEngine;

[Serializable]
public class SavedUnitPrefabEntry
{
    public string persistentId;
    public Unit prefab;
}

public class StageUnitPersistence : MonoBehaviour
{
    [SerializeField] private int playerFactionId = FactionRelations.Player1FactionId;
    [SerializeField] private bool restoreOnStart = true;
    [SerializeField] private FactionUnitTracker unitTracker;
    [SerializeField] private UnitSpawnPoint[] spawnPoints;
    [SerializeField] private SavedUnitPrefabEntry[] unitPrefabs;

    private void Awake()
    {
        playerFactionId = FactionRelations.UpgradeLegacyPlayerFactionId(playerFactionId);
    }

    private void Start()
    {
        if (restoreOnStart)
            SpawnSavedPlayerUnits();
    }

    public void SaveCurrentPlayerUnits()
    {
        GameProgress.GetOrCreate().SavePlayerUnitsFromScene(playerFactionId);
    }

    private void SpawnSavedPlayerUnits()
    {
        GameProgress progress = GameProgress.GetOrCreate();
        if (!progress.HasSavedUnits())
            return;

        int nextSpawnPointIndex = 0;

        foreach (UnitSaveData saveData in progress.SavedPlayerUnits)
        {
            Unit prefab = GetPrefab(saveData.persistentId);
            if (prefab == null)
            {
                Debug.LogWarning($"No unit prefab configured for saved unit '{saveData.persistentId}'. Add it to {nameof(StageUnitPersistence)}.");
                continue;
            }

            UnitSpawnPoint spawnPoint = GetNextAvailableSpawnPoint(ref nextSpawnPointIndex);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"No available spawn point for saved unit '{saveData.persistentId}'. Add more spawn points to this stage.");
                return;
            }

            spawnPoint.TrySpawn(prefab, saveData, playerFactionId, unitTracker, out _);
        }
    }

    private Unit GetPrefab(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId) || unitPrefabs == null)
            return null;

        foreach (SavedUnitPrefabEntry entry in unitPrefabs)
        {
            if (entry != null && entry.persistentId == persistentId)
                return entry.prefab;
        }

        return null;
    }

    private UnitSpawnPoint GetNextAvailableSpawnPoint(ref int startIndex)
    {
        if (spawnPoints == null)
            return null;

        while (startIndex < spawnPoints.Length)
        {
            UnitSpawnPoint spawnPoint = spawnPoints[startIndex];
            startIndex++;

            if (spawnPoint != null && !spawnPoint.IsOccupied)
                return spawnPoint;
        }

        return null;
    }
}
