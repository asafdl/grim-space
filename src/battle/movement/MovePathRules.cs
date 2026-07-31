using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public static class MovePathRules
{
	public static bool CanEndMovePath(ActorRuntime runtime)
	{
		if (!runtime.IsMovePathStarted)
			return true;

		if (runtime.MinPathApCost != 0)
			return false;

		// Retro braking waives the minimum paid-movement requirement.
		if (runtime.SpinBraked)
			return true;

		// MinPathApCost tracks step count (including free forward steps). PathApSpent
		// tracks real AP — a path can satisfy the step budget while spending < 3 AP.
		return runtime.PathApSpent == 0
			|| runtime.PathApSpent >= ActorRuntime.InitialMinPathApCost;
	}

	/// <summary>
	/// Prefer the branch that spends fewer move steps (free steps count), then less AP.
	/// </summary>
	public static bool PreferEndpointOption(Option candidate, Option existing) =>
		candidate.Path.Count < existing.Path.Count
		|| candidate.Path.Count == existing.Path.Count && candidate.ApCost < existing.ApCost;

	public static Option? ToEndpointOption(
		Coord origin,
		BodyFrame frame,
		IReadOnlyList<MoveStepAction> steps,
		ActorRuntime runtime,
		int baselinePathApSpent = 0)
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
			ApCost = runtime.PathApSpent - baselinePathApSpent,
			Path = path,
		};
	}
}
