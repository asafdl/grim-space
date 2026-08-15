using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Applies orientation button streamlining to the live planning queue.
/// </summary>
public static class OrientationStreamline
{
	public static bool TryApplyButton(BattleSimulation sim, IAction input)
	{
		var before = sim.Actions.ToList();
		bool IsCandidateLegal(IReadOnlyList<IAction> candidate) =>
			TryReplaceQueue(sim.Fork(), before, candidate);

		var after = input switch
		{
			HeadingTurnAction => HeadingDef.Instance.Streamline(before, input, IsCandidateLegal),
			RollAction => RollDef.Instance.Streamline(before, input, IsCandidateLegal),
			_ => null,
		};

		if (after is null)
			return false;

		return TryReplaceQueue(sim, before, after);
	}

	public static IReadOnlyList<IAction> StreamlineForCommit(IReadOnlyList<IAction> actions)
	{
		var afterYaw = HeadingDef.Instance.Streamline(actions, input: null, _ => true)!;
		return RollDef.Instance.Streamline(afterYaw, input: null, _ => true)!;
	}

	private static bool TryReplaceQueue(
		BattleSimulation sim,
		IReadOnlyList<IAction> before,
		IReadOnlyList<IAction> after)
	{
		var prefix = CommonPrefixLength(before, after);
		sim.Dequeue(prefix);
		return sim.TryEnqueue(keepRecords: true, actions: [..after.Skip(prefix)]);
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
