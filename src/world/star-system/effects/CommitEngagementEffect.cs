using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class CommitEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _firstUnitId;
	private readonly string _secondUnitId;

	public CommitEngagementEffect(string firstUnitId, string secondUnitId)
	{
		_firstUnitId = firstUnitId;
		_secondUnitId = secondUnitId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var first = world.StateOf(_firstUnitId);
		var second = world.StateOf(_secondUnitId);
		var initiatorId = first.CurrentEngagement?.Hunting is not null
			? _firstUnitId
			: second.CurrentEngagement?.Hunting is not null
				? _secondUnitId
				: _firstUnitId;

		var engagementId = first.CurrentEngagement?.Id
			?? second.CurrentEngagement?.Id
			?? TypedIdGenerator.NextId("engagement");

		var engagement = new Engagement(
			engagementId,
			EEngagementPhase.Engaged,
			initiatorId,
			new HashSet<string>([_firstUnitId, _secondUnitId], StringComparer.Ordinal),
			HuntedBy: null,
			Hunting: null);

		first.CurrentEngagement = engagement;
		second.CurrentEngagement = engagement;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
