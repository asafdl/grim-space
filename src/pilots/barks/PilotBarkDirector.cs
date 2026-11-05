namespace GrimSpace.Pilots.Barks;

/// <summary>
/// Gates and voices pilot barks. Scene-specific code decides who could bark and in what flavor;
/// this decides whether it actually happens (chance, global gap, per-pilot cooldown) and rolls the line.
/// Times are real seconds advanced via <see cref="Tick"/>.
/// </summary>
public sealed class PilotBarkDirector
{
	public const double DefaultChance = 0.7;
	public const double DefaultGlobalGapSeconds = 8.0;
	public const double DefaultPilotCooldownSeconds = 20.0;

	private readonly Func<string, string, bool> _say;
	private readonly Random _rng;
	private readonly double _globalGapSeconds;
	private readonly double _pilotCooldownSeconds;
	private readonly Dictionary<string, double> _pilotLastSpokeAt = new(StringComparer.Ordinal);
	private double _now;
	private double? _lastBarkAt;

	/// <param name="say">Presents a line for a pilot; returns false when it could not be shown.</param>
	public PilotBarkDirector(
		Func<string, string, bool> say,
		Random rng,
		double globalGapSeconds = DefaultGlobalGapSeconds,
		double pilotCooldownSeconds = DefaultPilotCooldownSeconds)
	{
		ArgumentNullException.ThrowIfNull(say);
		ArgumentNullException.ThrowIfNull(rng);
		ArgumentOutOfRangeException.ThrowIfNegative(globalGapSeconds);
		ArgumentOutOfRangeException.ThrowIfNegative(pilotCooldownSeconds);
		_say = say;
		_rng = rng;
		_globalGapSeconds = globalGapSeconds;
		_pilotCooldownSeconds = pilotCooldownSeconds;
	}

	public void Tick(double deltaSeconds)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(deltaSeconds);
		_now += deltaSeconds;
	}

	/// <summary>Barks a random line of <paramref name="flavor"/> with probability <paramref name="chance"/>, if not on cooldown.</summary>
	public bool Offer(string pilotId, EBarkFlavor flavor, double chance = DefaultChance)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentOutOfRangeException.ThrowIfNegative(chance);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(chance, 1.0);
		if (IsCoolingDown(pilotId) || _rng.NextDouble() >= chance)
			return false;

		return Speak(pilotId, PilotBarkCatalog.Roll(flavor, _rng));
	}

	/// <summary>Scripted line: bypasses chance and cooldowns, but still starts them.</summary>
	public bool Script(string pilotId, string line)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentException.ThrowIfNullOrWhiteSpace(line);
		return Speak(pilotId, line);
	}

	private bool IsCoolingDown(string pilotId) =>
		_now - _lastBarkAt < _globalGapSeconds
		|| (_pilotLastSpokeAt.TryGetValue(pilotId, out var spokeAt) && _now - spokeAt < _pilotCooldownSeconds);

	private bool Speak(string pilotId, string line)
	{
		if (!_say(pilotId, line))
			return false;

		_lastBarkAt = _now;
		_pilotLastSpokeAt[pilotId] = _now;
		return true;
	}
}
