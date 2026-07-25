namespace GrimSpace.Core.Engine;

public readonly record struct PeekFrame<TWorld, TRuntime>(
	TWorld World,
	ActorRuntimes<TRuntime> Runtimes)
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new();
