using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Units;

namespace GrimSpace.Battle.Units;

public sealed class Unit
{
	public Alliance Alliance { get; }
	public State State { get; }
	public IExecutionAgent<BattleWorld, ActorRuntime, Unit> ExecutionAgent { get; }

	public Unit(
		Alliance alliance,
		State state,
		IExecutionAgent<BattleWorld, ActorRuntime, Unit> executionAgent)
	{
		Alliance = alliance;
		State = state;
		ExecutionAgent = executionAgent;
	}

	public EUnitRelation RelationTo(Unit other)
	{
		if (other.State.Id == State.Id)
			return EUnitRelation.Self;

		return Alliance.IsAlliedWith(other.Alliance)
			? EUnitRelation.Ally
			: EUnitRelation.Opponent;
	}
}
