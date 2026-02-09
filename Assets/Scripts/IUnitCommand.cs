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
