public interface IUnitCommand { }

public readonly struct MoveCommand : IUnitCommand
{
    public readonly UnityEngine.Vector3 Destination;
    public MoveCommand(UnityEngine.Vector3 dest) => Destination = dest;
}

public readonly struct AttackCommand : IUnitCommand
{
    public readonly Unit Target;
    public AttackCommand(Unit target)
    {
        Target = target;
    }
}

public readonly struct FollowCommand : IUnitCommand
{
    public readonly Unit Target;
    public FollowCommand(Unit target)
    {
        Target = target;
    }
}

public readonly struct TalkCommand : IUnitCommand
{
    public readonly Unit Target;
    public readonly RecruitableUnit Recruitable;
    public readonly int FactionId;
    public readonly float TalkRange;

    public TalkCommand(Unit target, RecruitableUnit recruitable, int factionId, float talkRange)
    {
        Target = target;
        Recruitable = recruitable;
        FactionId = factionId;
        TalkRange = talkRange;
    }
}
