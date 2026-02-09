using UnityEngine;

public class UnitCommandReceiver : MonoBehaviour
{
    private UnitBrain brain;

    private void Awake()
    {
        brain = GetComponent<UnitBrain>();
    }

    public void SetCommand(IUnitCommand command)
    {
        brain.SetCommand(command);
    }
}
