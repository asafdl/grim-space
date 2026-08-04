namespace GrimSpace.Core.Dfs;

/// <summary>
/// Multi-dimensional budget Pareto frontier. Higher component values are better.
/// </summary>
internal static class BudgetFrontier
{
	/// <summary>
	/// Returns true when <paramref name="budget"/> is dominated by an existing vector and should be pruned.
	/// Otherwise updates the frontier and returns false.
	/// </summary>
	public static bool ShouldPrune(List<int[]> frontier, int[] budget)
	{
		for (var i = 0; i < frontier.Count; i++)
		{
			if (Dominates(frontier[i], budget))
				return true;
		}

		for (var i = frontier.Count - 1; i >= 0; i--)
		{
			if (Dominates(budget, frontier[i]))
				frontier.RemoveAt(i);
		}

		frontier.Add((int[])budget.Clone());
		return false;
	}

	public static bool Dominates(int[] left, int[] right)
	{
		for (var i = 0; i < right.Length; i++)
		{
			if (left[i] < right[i])
				return false;
		}

		return true;
	}
}
