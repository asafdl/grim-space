namespace GrimSpace.Units.Heat;

public sealed class HeatSpec
{
	public int? Capacity { get; }
	public int ClearPerTurn { get; }
	public float GainMultiplier { get; }

	public bool Participates => Capacity is > 0;

	public int EffectiveCapacity => Capacity is > 0 ? Capacity.Value : 0;

	public HeatSpec(int? capacity, int clearPerTurn, float gainMultiplier)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(clearPerTurn);
		ArgumentOutOfRangeException.ThrowIfNegative(gainMultiplier);
		if (capacity is < 0)
			throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity cannot be negative.");

		Capacity = capacity;
		ClearPerTurn = clearPerTurn;
		GainMultiplier = gainMultiplier;
	}

	public int ScaleGain(int baseHeat)
	{
		if (!Participates || baseHeat == 0 || GainMultiplier == 0f)
			return 0;

		return (int)System.Math.Round(baseHeat * (double)GainMultiplier, MidpointRounding.AwayFromZero);
	}

	public static HeatSpec Standard { get; } = new(100, 20, 1f);

	public static HeatSpec None { get; } = new(null, 0, 1f);
}
