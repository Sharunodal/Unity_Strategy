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
        if (clicked == null)
            return;

        RecruitableUnit recruitable = clicked.GetComponent<RecruitableUnit>();
        if (recruitable != null && recruitable.CanOpenForFaction(localPlayerId))
        {
            foreach (var receiver in selection.GetSelectedCommandReceivers())
            {
                Unit unit = receiver.GetComponent<Unit>();
                if (unit == null || unit.ownerId != localPlayerId)
                    continue;

                receiver.SetCommand(new TalkCommand(clicked, recruitable, localPlayerId, recruitable.ConversationRange));
                return;
            }

            return;
        }

        bool isFriendly = FactionRelations.AreFriendly(localPlayerId, clicked.ownerId);
        bool isHostile = FactionRelations.AreHostile(localPlayerId, clicked.ownerId);

        if (!isFriendly && !isHostile)
            return;

        foreach (var receiver in selection.GetSelectedCommandReceivers())
        {
            if (isFriendly)
                receiver.SetCommand(new FollowCommand(clicked));
            else
                receiver.SetCommand(new AttackCommand(clicked));
        }
    }
}
