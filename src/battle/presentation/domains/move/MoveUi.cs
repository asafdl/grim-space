using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

public static class MoveUi
{
	internal static bool CanReopenLastMove(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<int?> undoGroups,
		string actorId)
	{
		if (actions.Count == 0)
			return false;

		var startIndex = actions.Count - 1;
		if (undoGroups[startIndex] is int groupId)
		{
			while (startIndex > 0 && undoGroups[startIndex - 1] == groupId)
				startIndex--;
		}

		var hasMoveStep = false;
		for (var i = startIndex; i < actions.Count; i++)
		{
			var action = actions[i];
			if (action.ActorId != actorId || !MovePathIndex.IsMovementAction(action))
				return false;

			if (action is MoveStepAction)
				hasMoveStep = true;
		}

		return hasMoveStep;
	}

	public static (IReadOnlyList<MoveCheckpoint> Checkpoints, Coord? Target) GetPathHighlights(
		IReadOnlyList<MovePathOption> paths,
		MovePathOption? hovered,
		IReadOnlyList<MoveCheckpoint> committedPath,
		MovePathOption? selected = null)
	{
		if (selected is not null)
			return (selected.Checkpoints.Skip(1).ToList(), selected.EndPosition);

		if (hovered is not null)
			return (hovered.Checkpoints.Skip(1).ToList(), hovered.EndPosition);

		if (committedPath.Count > 0)
			return (committedPath, committedPath[^1].Position);

		return ([], null);
	}
}
