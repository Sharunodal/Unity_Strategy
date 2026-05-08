using UnityEngine;

public class StageUnitPersistence : MonoBehaviour
{
    [SerializeField] private int playerFactionId = FactionRelations.Player1FactionId;
    [SerializeField] private bool restoreOnStart = true;

    private void Awake()
    {
        playerFactionId = FactionRelations.UpgradeLegacyPlayerFactionId(playerFactionId);
    }

    private void Start()
    {
        if (restoreOnStart)
            GameProgress.GetOrCreate().ApplySavedUnitsToScene(playerFactionId);
    }

    public void SaveCurrentPlayerUnits()
    {
        GameProgress.GetOrCreate().SavePlayerUnitsFromScene(playerFactionId);
    }
}
