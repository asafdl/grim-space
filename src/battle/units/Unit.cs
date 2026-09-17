using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Unit
{
	public State State { get; }
	public ExecutionAgent<BattleWorld, ActorRuntime> ExecutionAgent { get; }
	public ETeam Team { get; }

	public Unit(
		State state,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent,
		ETeam team)
	{
		State = state;
		ExecutionAgent = executionAgent;
		Team = team;
	}

	public EUnitRelation RelationTo(Unit other) {
		if (other.State.Id == State.Id)
			return EUnitRelation.Self;
		if (other.Team == Team)
			return EUnitRelation.Ally;
		return EUnitRelation.Opponent;
	}
}
