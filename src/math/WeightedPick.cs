namespace GrimSpace.Math;

public static class WeightedPick
{
	/// <param name="unitRoll">Uniform value in [0, 1).</param>
	public static int Index<T>(IReadOnlyList<T> items, Func<T, double> weightOf, double unitRoll)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(weightOf);
		if (items.Count == 0)
			throw new ArgumentException("At least one item is required.", nameof(items));

		var total = 0.0;
		foreach (var item in items)
			total += weightOf(item);
		if (total <= 0)
			throw new ArgumentException("Total weight must be positive.", nameof(items));

		var target = unitRoll * total;
		var cumulative = 0.0;
		for (var index = 0; index < items.Count; index++)
		{
			cumulative += weightOf(items[index]);
			if (target < cumulative)
				return index;
		}

		return items.Count - 1;
	}

	public static T Of<T>(IReadOnlyList<T> items, Func<T, double> weightOf, double unitRoll) =>
		items[Index(items, weightOf, unitRoll)];
}
