using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Collapses consecutive yaw/roll spam into net-equivalent actions while planning.
/// </summary>
public static class OrientationStreamline
{
	public static IReadOnlyList<IAction> Compact(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<int?> undoGroups)
	{
		var afterYaw = HeadingDef.Instance.Streamline(actions, undoGroups);
		return RollDef.Instance.Streamline(afterYaw, new int?[afterYaw.Count]);
	}

	public static void CompactQueue(BattleSimulation sim)
	{
		var before = sim.Actions.ToList();
		var after = Compact(before, sim.UndoGroups.ToList());
		if (SameActions(before, after))
			return;

		var prefix = CommonPrefixLength(before, after);
		sim.Dequeue(prefix);
		foreach (var action in after.Skip(prefix))
		{
			if (!sim.TryEnqueue(keepRecords: true, action))
				throw new InvalidOperationException("Orientation streamline produced an illegal queue.");
		}
	}

	private static bool SameActions(IReadOnlyList<IAction> left, IReadOnlyList<IAction> right)
	{
		if (left.Count != right.Count)
			return false;
		for (var i = 0; i < left.Count; i++)
		{
			if (!Equals(left[i], right[i]))
				return false;
		}

		return true;
	}

	private static int CommonPrefixLength(IReadOnlyList<IAction> left, IReadOnlyList<IAction> right)
	{
		var n = System.Math.Min(left.Count, right.Count);
		var i = 0;
		while (i < n && Equals(left[i], right[i]))
			i++;
		return i;
	}
}
