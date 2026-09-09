using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
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
		var initiatorId = first.EngagementTargetUnitId is not null ? _firstUnitId : _secondUnitId;

		first.EngagementTargetUnitId = null;
		second.EngagementTargetUnitId = null;
		first.HuntedByUnitId = null;
		second.HuntedByUnitId = null;
		first.EngagementPhase = EEngagementPhase.Engaged;
		second.EngagementPhase = EEngagementPhase.Engaged;
		first.EngagementInitiatorUnitId = initiatorId;
		second.EngagementInitiatorUnitId = initiatorId;
		first.ResolvedEngagementOutcome = null;
		second.ResolvedEngagementOutcome = null;
		first.ClearEngagedWith();
		second.ClearEngagedWith();
		first.AddEngagedWith(_secondUnitId);
		second.AddEngagedWith(_firstUnitId);
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
