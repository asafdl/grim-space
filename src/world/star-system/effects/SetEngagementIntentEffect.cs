using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
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
		var target = world.StateOf(_targetId);

		if (initiator.CurrentEngagement?.Hunting is { } priorTargetId
			&& world.FleetRegistry.TryGet(priorTargetId, out var priorTarget))
			EngagementState.ClearHuntedBy(priorTarget.State, _initiatorId);

		var engagementId = initiator.CurrentEngagement?.Id ?? TypedIdGenerator.NextId("engagement");
		initiator.CurrentEngagement = new Engagement(
			engagementId,
			EEngagementPhase.Pursuing,
			_initiatorId,
			new HashSet<string>([_initiatorId], StringComparer.Ordinal),
			HuntedBy: null,
			Hunting: _targetId);

		target.CurrentEngagement = new Engagement(
			engagementId,
			EEngagementPhase.None,
			_initiatorId,
			new HashSet<string>(StringComparer.Ordinal),
			HuntedBy: _initiatorId,
			Hunting: null);

		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
