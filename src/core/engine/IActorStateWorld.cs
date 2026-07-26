namespace GrimSpace.Core.Engine;

public interface IActorStateWorld<TState, TSelf> : IWorld<TSelf>
	where TSelf : IWorld<TSelf>
{
	TState StateOf(string actorId);
}
