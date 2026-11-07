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

	public PilotPersonality ApplyModifiers(params PilotPersonality[] modifiers)
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
}
