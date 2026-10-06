using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contact;
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
		var priorTargetId = initiator.CurrentEngagement?.Hunting;
		var records = new List<IRecord>();

		if (priorTargetId is not null
			&& !string.Equals(priorTargetId, _targetId, StringComparison.Ordinal))
		{
			if (world.FleetRegistry.TryGet(priorTargetId, out var priorTarget))
				EngagementState.ClearHuntedBy(priorTarget.State, _initiatorId);
			records.Add(new Record<FleetPursuitChanged>(
				new FleetPursuitChanged(_initiatorId, priorTargetId, false)));
		}

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

		if (!string.Equals(priorTargetId, _targetId, StringComparison.Ordinal))
		{
			records.Add(new Record<FleetPursuitChanged>(
				new FleetPursuitChanged(_initiatorId, _targetId, true)));
		}

		return records;
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
