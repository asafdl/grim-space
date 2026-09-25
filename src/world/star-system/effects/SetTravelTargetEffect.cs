using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetTravelTargetEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _actorId;
	private readonly TravelTarget _travelTarget;

	public SetTravelTargetEffect(string actorId, TravelTarget travelTarget)
	{
		_actorId = actorId;
		_travelTarget = travelTarget;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.StateOf(_actorId).TravelTarget = _travelTarget;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
