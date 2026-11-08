using System.Text.Json.Serialization;

namespace GrimSpace.Pilots;

public readonly record struct PilotPersonality
{
	public const int MinValue = -5;
	public const int MaxValue = 5;

	/// <summary>-5 is fearful; +5 is aggressive.</summary>
	public int ThreatResponse { get; }

	/// <summary>-5 is impulsive; +5 is calculated.</summary>
	public int Deliberation { get; }

	/// <summary>-5 is self-interested; +5 is team-loyal.</summary>
	public int Allegiance { get; }

	[JsonConstructor]
	public PilotPersonality(int threatResponse, int deliberation, int allegiance)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(threatResponse, MinValue);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(threatResponse, MaxValue);
		ArgumentOutOfRangeException.ThrowIfLessThan(deliberation, MinValue);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(deliberation, MaxValue);
		ArgumentOutOfRangeException.ThrowIfLessThan(allegiance, MinValue);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(allegiance, MaxValue);

		ThreatResponse = threatResponse;
		Deliberation = deliberation;
		Allegiance = allegiance;
	}

	public PilotPersonality ApplyModifiers(params PilotPersonalityModifier[] modifiers)
	{
		ArgumentNullException.ThrowIfNull(modifiers);
		long threatResponse = ThreatResponse;
		long deliberation = Deliberation;
		long allegiance = Allegiance;
		foreach (var modifier in modifiers)
		{
			threatResponse += modifier.ThreatResponse;
			deliberation += modifier.Deliberation;
			allegiance += modifier.Allegiance;
		}

		return new(
			(int)System.Math.Clamp(threatResponse, MinValue, MaxValue),
			(int)System.Math.Clamp(deliberation, MinValue, MaxValue),
			(int)System.Math.Clamp(allegiance, MinValue, MaxValue));
	}

	public int ValueOf(EPilotPersonalityAxis axis) =>
		axis switch
		{
			EPilotPersonalityAxis.ThreatResponse => ThreatResponse,
			EPilotPersonalityAxis.Deliberation => Deliberation,
			EPilotPersonalityAxis.Allegiance => Allegiance,
			_ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unknown personality axis."),
		};

	public int ResponseWeight(EPilotPersonalityAxis axis, bool positiveSide)
	{
		var value = ValueOf(axis);
		if (value == 0)
			return 1;
		return (value > 0) == positiveSide ? value * value : 0;
	}

	/// <summary>Shifts a [0,1] chance by up to <paramref name="maxSwing"/> along <paramref name="axis"/>.</summary>
	public double ShiftedChance(double baseChance, double maxSwing, EPilotPersonalityAxis axis, bool positiveSide)
	{
		var lean = ValueOf(axis) / (double)MaxValue;
		if (!positiveSide)
			lean = -lean;

		return System.Math.Clamp(baseChance + maxSwing * lean, 0, 1);
	}

	/// <summary>Returns null when every option opposes the personality; options remain consumer-owned.</summary>
	public int? SelectResponseIndex<T>(
		IReadOnlyList<T> options,
		Func<T, (EPilotPersonalityAxis Axis, bool PositiveSide)> affinity,
		Random rng)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(affinity);
		ArgumentNullException.ThrowIfNull(rng);
		if (options.Count == 0)
			throw new ArgumentException("At least one response option is required.", nameof(options));

		var weights = new int[options.Count];
		var totalWeight = 0;
		for (var index = 0; index < options.Count; index++)
		{
			var (axis, positiveSide) = affinity(options[index]);
			weights[index] = ResponseWeight(axis, positiveSide);
			totalWeight = checked(totalWeight + weights[index]);
		}
		if (totalWeight == 0)
			return null;

		var roll = rng.Next(totalWeight);
		for (var index = 0; index < weights.Length; index++)
		{
			if (roll < weights[index])
				return index;
			roll -= weights[index];
		}
		throw new InvalidOperationException("Personality response roll exceeded its total weight.");
	}
}
