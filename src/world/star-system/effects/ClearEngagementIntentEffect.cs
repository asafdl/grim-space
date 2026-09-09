using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ClearEngagementIntentEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _initiatorId;

	public ClearEngagementIntentEffect(string initiatorId) => _initiatorId = initiatorId;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var initiator = world.StateOf(_initiatorId);
		if (initiator.EngagementTargetUnitId is not { } targetId)
			return [];

		initiator.EngagementTargetUnitId = null;
		if (initiator.EngagementPhase is EEngagementPhase.Pursuing or EEngagementPhase.AwaitingDecision)
			initiator.EngagementPhase = EEngagementPhase.None;

		if (world.UnitRegistry.TryGet(targetId, out var target))
			target.State.HuntedByUnitId = null;

		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
