using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public static class MovePathRules
{
	public static bool CanEndMovePath(ActorRuntime runtime) =>
		!runtime.IsMovePathStarted || runtime.MinPathApCost == 0 || runtime.PathApSpent == 0;

	public static Option? ToEndpointOption(
		Coord origin,
		BodyFrame frame,
		IReadOnlyList<MoveStepAction> steps,
		ActorRuntime runtime)
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
			StartMomentumLevel = runtime.MoveStartMomentumLevel,
			EndMomentumLevel = runtime.MovementBuildupLevel,
		};
	}
}
