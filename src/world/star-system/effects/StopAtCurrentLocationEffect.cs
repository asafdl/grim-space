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
		var unit = world.FleetRegistry.FleetOf(_unitId);
		var position = unit.State.PositionAt(world, runtime.CachedPath, 0).Position;

		var records = new List<IRecord>();
		foreach (var effect in TravelFuelEffects.SettleBeforeStop(world, runtime, _unitId))
			records.AddRange(effect.Apply(world, runtime, actorId));

		CancelPendingMoveEffect.Instance.Apply(world, runtime, actorId);
		ClearJourneyRuntimeEffect.Instance.Apply(world, runtime, actorId);
		records.AddRange(UpdateLocationEffect.StopAt(_unitId, position).Apply(world, runtime, actorId));
		return records;
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
