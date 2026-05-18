using UnityEngine;

public class UnitSpawnPoint : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    public bool TrySpawn(Unit prefab, UnitSaveData saveData, int playerFactionId, FactionUnitTracker tracker, out Unit spawnedUnit)
    {
        spawnedUnit = null;

        if (IsOccupied || prefab == null || saveData == null)
            return false;

        spawnedUnit = Instantiate(prefab, transform.position, transform.rotation);
        spawnedUnit.ApplySaveData(saveData);
        spawnedUnit.SetOwnerId(playerFactionId);
        spawnedUnit.SetTracker(tracker);

        IsOccupied = true;
        return true;
    }
}
