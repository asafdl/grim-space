using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetEngagementIntentEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _initiatorId;
	private readonly string _targetId;

	public SetEngagementIntentEffect(string initiatorId, string targetId)
	{
		_initiatorId = initiatorId;
		_targetId = targetId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var initiator = world.StateOf(_initiatorId);
		if (initiator.EngagementTargetUnitId is { } priorTargetId
			&& world.UnitRegistry.TryGet(priorTargetId, out var priorTarget))
			priorTarget.State.HuntedByUnitId = null;

		initiator.EngagementTargetUnitId = _targetId;
		initiator.EngagementPhase = EEngagementPhase.Pursuing;
		initiator.ResolvedEngagementOutcome = null;
		world.StateOf(_targetId).HuntedByUnitId = _initiatorId;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
