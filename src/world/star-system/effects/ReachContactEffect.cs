using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ReachContactEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _initiatorId;
	private readonly string _targetId;

	public ReachContactEffect(string initiatorId, string targetId)
	{
		_initiatorId = initiatorId;
		_targetId = targetId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.StateOf(_initiatorId).EngagementPhase = EEngagementPhase.AwaitingDecision;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
