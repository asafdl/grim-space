using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public static class MovePathRules
{
	// TODO: Whole-prefix move legality belongs in the effect/runtime model, not a
	// post-hoc helper. Per-step IsLegal can enqueue prefixes that are stuck mid-path
	// (not a valid stop, no legal next step). Fold this into effects or post-apply
	// invariants once the action pipeline owns composite path state.

	public static bool CanEndMovePath(ActorSession runtime) =>
		!runtime.IsMovePathStarted || runtime.MinPathApCost == 0 || runtime.PathApSpent == 0;

	public static bool IsValidMovePrefix(BattleBoard world, ActorSession runtime, string actorId)
	{
		if (!runtime.IsMovePathStarted)
			return true;

		if (CanEndMovePath(runtime))
			return true;

		foreach (var candidate in MoveDef.Instance.Discover(world, runtime, actorId))
		{
			if (MoveDef.Instance.IsPossible(candidate, world, runtime))
				return true;
		}

		return false;
	}

	public static Option? ToEndpointOption(
		Coord origin,
		BodyFrame frame,
		IReadOnlyList<MoveStepAction> steps,
		ActorSession runtime)
	{
		if (steps.Count == 0 || !CanEndMovePath(runtime))
			return null;

		var path = new List<Coord>(steps.Count);
		var position = origin;

		foreach (var step in steps)
		{
			position += frame.Step(step.Direction);
			path.Add(position);
		}

		return new Option
		{
			ApCost = runtime.PathApSpent,
			Path = path,
		};
	}
}
