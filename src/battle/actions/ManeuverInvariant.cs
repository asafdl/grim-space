using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

internal static class ManeuverInvariant
{
	public static InvariantStatus Evaluate(
		BattleWorld world,
		ActorRuntime runtime,
		IReadOnlyList<IAction> actions,
		string actorId)
	{
		var progress = ProgressOf(actions, actorId);
		if (progress == ManeuverProgress.None)
			return InvariantStatus.Ok;
		if (progress == ManeuverProgress.Invalid)
			return InvariantStatus.Impossible;

		return CompletionActions(actorId, progress)
			.Any(action => MoveDef.Instance.IsLegal(action, world, runtime))
			? InvariantStatus.Incomplete
			: InvariantStatus.Impossible;
	}

	public static ManeuverProgress ProgressOf(IReadOnlyList<IAction> actions, string actorId)
	{
		var progress = ManeuverProgress.None;
		foreach (var action in actions.Where(action => action.ActorId == actorId))
		{
			progress = (progress, action) switch
			{
				(ManeuverProgress.None, HeadingTurnAction) => ManeuverProgress.Heading,
				(ManeuverProgress.None, RollAction) => ManeuverProgress.Roll,
				(ManeuverProgress.None, _) => ManeuverProgress.None,
				(ManeuverProgress.Heading, RollAction) => ManeuverProgress.HeadingRoll,
				(ManeuverProgress.Heading or ManeuverProgress.HeadingRoll,
					MoveStepAction { Direction: ESpatialOrientation.Forward }) => ManeuverProgress.None,
				(ManeuverProgress.Roll, MoveStepAction) => ManeuverProgress.None,
				_ => ManeuverProgress.Invalid,
			};
			if (progress == ManeuverProgress.Invalid)
				return progress;
		}

		return progress;
	}

	private static IEnumerable<MoveStepAction> CompletionActions(
		string actorId,
		ManeuverProgress progress) =>
		progress == ManeuverProgress.Roll
			? Enum.GetValues<ESpatialOrientation>()
				.Select(direction => new MoveStepAction(actorId, direction))
			: [new MoveStepAction(actorId)];
}

internal enum ManeuverProgress
{
	None,
	Heading,
	Roll,
	HeadingRoll,
	Invalid,
}
