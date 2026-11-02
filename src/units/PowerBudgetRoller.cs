namespace GrimSpace.Units;

internal static class PowerBudgetRoller
{
	public static List<T> Spend<T>(
		int budget,
		Func<int, IReadOnlyList<T>> options,
		Func<T, int> cost,
		Func<int, int>? pickIndex,
		IComparer<T>? canonicalOrder,
		Action<T>? onPicked = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(cost);
		if (budget < 0)
			throw new ArgumentOutOfRangeException(nameof(budget));

		var picked = new List<T>();
		var remaining = budget;
		while (remaining > 0)
		{
			var affordable = new List<T>();
			foreach (var option in options(remaining))
			{
				var optionCost = cost(option);
				if (optionCost <= 0)
					throw new InvalidOperationException("Power budget options must cost at least 1.");
				if (optionCost <= remaining)
					affordable.Add(option);
			}

			if (affordable.Count == 0)
				break;

			T pick;
			if (pickIndex is null)
			{
				affordable.Sort(canonicalOrder ?? Comparer<T>.Default);
				pick = affordable[0];
			}
			else
			{
				var index = pickIndex(affordable.Count);
				if ((uint)index >= (uint)affordable.Count)
					throw new ArgumentOutOfRangeException(nameof(pickIndex));
				pick = affordable[index];
			}

			picked.Add(pick);
			onPicked?.Invoke(pick);
			remaining -= cost(pick);
		}

		return picked;
	}
}
