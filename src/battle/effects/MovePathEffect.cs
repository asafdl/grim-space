using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MovePathEffect(string ownerId, IReadOnlyList<Coord> path)
	: IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var actor = world.StateOf(ownerId);
		var steps = MoveDef.StepsFromPath(ownerId, BodyFrame.From(actor), actor.Position, path);

		foreach (var step in steps)
			((IAction<BattleBoard, ActorSession>)step).Apply(world, runtime);
	}
}
