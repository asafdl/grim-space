using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ResolveEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _firstUnitId;
	private readonly string _secondUnitId;
	private readonly BattleOutcome _outcome;

	public ResolveEngagementEffect(string firstUnitId, string secondUnitId, BattleOutcome outcome)
	{
		_firstUnitId = firstUnitId;
		_secondUnitId = secondUnitId;
		_outcome = outcome;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		ResolveWithOutcome(world.StateOf(_firstUnitId));
		ResolveWithOutcome(world.StateOf(_secondUnitId));
		return [];
	}

	private void ResolveWithOutcome(State state)
	{
		state.EngagementTargetUnitId = null;
		state.HuntedByUnitId = null;
		state.EngagementInitiatorUnitId = null;
		state.ClearEngagedWith();
		state.EngagementPhase = EEngagementPhase.Resolved;
		state.ResolvedEngagementOutcome = _outcome;
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
