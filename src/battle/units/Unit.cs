using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public abstract class Unit
{
	public EController Controller { get; }
	public State State { get; }
	public IExecutionAgent<BattleWorld, ActorRuntime, Unit> ExecutionAgent { get; }

	protected Unit(
		EController controller,
		State state,
		IExecutionAgent<BattleWorld, ActorRuntime, Unit> executionAgent)
	{
		Controller = controller;
		State = state;
		ExecutionAgent = executionAgent;
	}
}
