using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class StopAtCurrentLocationEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly string _unitId;

	public StopAtCurrentLocationEffect(string unitId) => _unitId = unitId;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		var unit = world.UnitRegistry.UnitOf(_unitId);
		var position = MoveDef.ResolveOrigin(world, unit, runtime);

		CancelPendingMoveEffect.Instance.Apply(world, runtime, actorId);
		ClearJourneyRuntimeEffect.Instance.Apply(world, runtime, actorId);
		return UpdateLocationEffect.ArriveAtCoord(_unitId, position).Apply(world, runtime, actorId);
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
