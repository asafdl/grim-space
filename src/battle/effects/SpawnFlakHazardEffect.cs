using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class SpawnFlakHazardEffect(string hazardId, IEnumerable<Coord> cells)
	: IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var ownerFrame = BodyFrame.From(world.StateOf(actorId));
		var hazard = Hazard.FlakBurst(hazardId, actorId, ownerFrame, cells);
		world.MutableNonUnits[hazard.Id] = hazard;
	}
}
