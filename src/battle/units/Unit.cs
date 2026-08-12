using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Units;

namespace GrimSpace.Battle.Units;

public sealed class Unit
{
	public Alliance Alliance { get; }
	public State State { get; }
	public ExecutionAgent<BattleWorld, ActorRuntime> ExecutionAgent { get; }

	public Unit(
		Alliance alliance,
		State state,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent)
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
