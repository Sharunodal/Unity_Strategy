using UnityEngine;

public class CommandSystem : MonoBehaviour
{
    private float spreadRadius = 1.5f;

    public void IssueMoveCommand(SelectionManager selection, Vector3 destination)
    {
        int i = 0;
        foreach (var receiver in selection.GetSelectedCommandReceivers())
        {
            Vector3 offset = (i == 0) ? Vector3.zero : Random.insideUnitSphere * spreadRadius;
            offset.y = 0f;

            receiver.SetCommand(new MoveCommand(destination + offset));
            i++;
        }
    }

    public void IssueFollowOrAttackCommand(SelectionManager selection, Unit clicked, int localPlayerId)
    {
        bool isFriendly = clicked.ownerId == localPlayerId;

        foreach (var receiver in selection.GetSelectedCommandReceivers())
        {
            if (isFriendly)
                receiver.SetCommand(new FollowCommand(clicked));
            else
                receiver.SetCommand(new AttackCommand(clicked));
        }
    }
}
