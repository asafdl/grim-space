using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ResolveEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _victorFleetId;
	private readonly string _defeatedFleetId;

	public ResolveEngagementEffect(
		string victorFleetId,
		string defeatedFleetId)
	{
		_victorFleetId = victorFleetId;
		_defeatedFleetId = defeatedFleetId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		ResolveWithOutcome(world.StateOf(_victorFleetId), EBattleParticipantState.Alive);
		ResolveWithOutcome(world.StateOf(_defeatedFleetId), EBattleParticipantState.Destroyed);
		CancelPendingMoveEffect.Instance.Apply(world, runtime, _defeatedFleetId);
		world.Timeline.CancelPendingForActor(_defeatedFleetId);
		runtime.Reset();
		world.FleetRegistry.Remove(_defeatedFleetId);
		return [];
	}

	private static void ResolveWithOutcome(State state, EBattleParticipantState participantState)
	{
		state.EngagementTargetUnitId = null;
		state.HuntedByUnitId = null;
		state.EngagementInitiatorUnitId = null;
		state.ClearEngagedWith();
		state.EngagementPhase = EEngagementPhase.Resolved;
		state.ResolvedEngagementState = participantState;
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
