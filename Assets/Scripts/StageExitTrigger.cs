using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class StageExitTrigger : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Stage 2";
    [SerializeField] private int nextStageNumber = 2;
    [SerializeField] private int playerFactionId = FactionRelations.Player1FactionId;
    [SerializeField] private bool requiresObjectiveComplete = true;
    [SerializeField] private bool objectiveComplete = true;
    [SerializeField] private bool requireAllLivingPlayerUnitsInside = false;
    [SerializeField] private int requiredPlayerUnitsInside = 1;

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

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
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
            neededUnits = CountLivingPlayerUnitsInScene();

        if (playerUnitsInside.Count < Mathf.Max(1, neededUnits))
            return;

        loading = true;

        GameProgress progress = GameProgress.GetOrCreate();
        progress.SavePlayerUnitsFromScene(playerFactionId);
        progress.UnlockStage(nextStageNumber);

        SceneManager.LoadScene(nextSceneName);
    }

    private int CountLivingPlayerUnitsInScene()
    {
        int count = 0;
        Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude);

        foreach (Unit unit in units)
        {
            if (unit != null && unit.ownerId == playerFactionId && unit.currentHitpoints > 0f)
                count++;
        }

        return count;
    }

    private void RemoveDestroyedUnits()
    {
        playerUnitsInside.RemoveWhere(unit => unit == null || unit.ownerId != playerFactionId || unit.currentHitpoints <= 0f);
    }
}
