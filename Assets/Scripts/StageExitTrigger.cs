using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class StageExitTrigger : MonoBehaviour
{
    [SerializeField] private FactionUnitTracker unitTracker;
    [SerializeField] private string nextSceneName = "Stage 2";
    [SerializeField] private int nextStageNumber = 2;
    [SerializeField] private bool requiresObjectiveComplete = true;
    [SerializeField] private bool objectiveComplete = true;
    [SerializeField] private bool requireAllLivingPlayerUnitsInside = false;
    [SerializeField] private int requiredPlayerUnitsInside = 1;
    
    private int playerFactionId = FactionRelations.Player1FactionId;
    private readonly HashSet<Unit> playerUnitsInside = new();
    private bool loading;

    private void Awake()
    {
        playerFactionId = FactionRelations.UpgradeLegacyPlayerFactionId(playerFactionId);
    }

    public void SetObjectiveComplete(bool complete)
    {
        objectiveComplete = complete;
        TryLoadNextStage();
    }

    private void OnTriggerEnter(Collider other)
    {
        Unit unit = other.GetComponentInParent<Unit>();
        if (unit != null && unit.ownerId == playerFactionId && unit.currentHitpoints > 0f)
            playerUnitsInside.Add(unit);

        TryLoadNextStage();
    }

    private void OnTriggerStay(Collider other)
    {
        Unit unit = other.GetComponentInParent<Unit>();
        if (unit != null && unit.ownerId == playerFactionId && unit.currentHitpoints > 0f)
            playerUnitsInside.Add(unit);

        TryLoadNextStage();
    }

    private void OnTriggerExit(Collider other)
    {
        Unit unit = other.GetComponentInParent<Unit>();
        if (unit != null)
            playerUnitsInside.Remove(unit);
    }

    private void TryLoadNextStage()
    {
        if (loading)
            return;

        if (requiresObjectiveComplete && !objectiveComplete)
            return;

        RemoveDestroyedUnits();

        int neededUnits = requiredPlayerUnitsInside;
        if (requireAllLivingPlayerUnitsInside)
            neededUnits = GetLivingPlayerUnitCount();

        if (playerUnitsInside.Count < Mathf.Max(1, neededUnits))
            return;

        loading = true;

        GameProgress progress = GameProgress.GetOrCreate();
        progress.SavePlayerUnitsFromScene(playerFactionId);
        progress.UnlockStage(nextStageNumber);

        SceneManager.LoadScene(nextSceneName);
    }

    private int GetLivingPlayerUnitCount()
    {
        if (unitTracker != null)
            return unitTracker.GetLivingUnitCount(playerFactionId);

        return requiredPlayerUnitsInside;
    }

    private void RemoveDestroyedUnits()
    {
        playerUnitsInside.RemoveWhere(unit => unit == null || unit.ownerId != playerFactionId || unit.currentHitpoints <= 0f);
    }
}
