using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;

namespace GrimSpace.Pilots.Barks;

/// <summary>
/// Subscribes to game events, evaluates bark context, and voices pilots through registered handles.
/// Times are real seconds advanced via <see cref="Tick"/>.
/// </summary>
public sealed class PilotBarkDirector : IDisposable
{
	private static readonly EBarkFlavor[] DeliveryCompletionFlavors =
		[EBarkFlavor.Disdain, EBarkFlavor.Relieved];

	public const double DefaultChance = 0.7;
	public const double DefaultGlobalGapSeconds = 8.0;
	public const double DefaultPilotCooldownSeconds = 20.0;

	private readonly Dictionary<string, Func<string, bool>> _pilotHandles = new(StringComparer.Ordinal);
	private readonly State? _run;
	private IDisposable? _deliverySubscription;
	private bool _openingChecked;
	private readonly Random _rng;
	private readonly double _globalGapSeconds;
	private readonly double _pilotCooldownSeconds;
	private readonly Dictionary<string, double> _pilotLastSpokeAt = new(StringComparer.Ordinal);
	private double _now;
	private double? _lastBarkAt;

	public PilotBarkDirector(
		Random rng,
		double globalGapSeconds = DefaultGlobalGapSeconds,
		double pilotCooldownSeconds = DefaultPilotCooldownSeconds)
	{
		ArgumentNullException.ThrowIfNull(rng);
		ArgumentOutOfRangeException.ThrowIfNegative(globalGapSeconds);
		ArgumentOutOfRangeException.ThrowIfNegative(pilotCooldownSeconds);
		_rng = rng;
		_globalGapSeconds = globalGapSeconds;
		_pilotCooldownSeconds = pilotCooldownSeconds;
	}

	public PilotBarkDirector(State run, Random rng) : this(rng)
	{
		ArgumentNullException.ThrowIfNull(run);
		_run = run;
		BindStarSystem();
	}

	internal void BindStarSystem()
	{
		if (_run is null)
			throw new InvalidOperationException("Cannot subscribe without a bound run.");
		_deliverySubscription?.Dispose();
		_deliverySubscription = _run.StarSystem
			.Subscribe<Record<ContractStateChanged>>(OnDeliveryContractStateChanged);
	}

	public void RegisterPilot(string pilotId, Func<string, bool> say)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentNullException.ThrowIfNull(say);
		_pilotHandles.Add(pilotId, say);
	}

	public void UnregisterPilot(string pilotId) => _pilotHandles.Remove(pilotId);

	public void Dispose()
	{
		_deliverySubscription?.Dispose();
		_pilotHandles.Clear();
	}

	public void Tick(double deltaSeconds)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(deltaSeconds);
		_now += deltaSeconds;
		if (_run is null || _openingChecked || _pilotHandles.Count == 0)
			return;

		_openingChecked = true;
		if (_run.TryConsumeOpening())
			PlayOpeningBarks(_run);
	}

	private void OnDeliveryContractStateChanged(Record<ContractStateChanged> record)
	{
		var change = record.Value;
		if (_run is not { } run
			|| change.HolderUnitId != State.PlayerFleetUnitId
			|| change.Status != EContractStatus.Completed)
		{
			GameLog.Log("[Barks] Ignore: not a player contract completion.");
			return;
		}
		if (!run.StarSystem.Map.ContractRegistry.TryGet(change.ContractId, out var contract))
		{
			GameLog.Log("[Barks] Ignore: contract not found.");
			return;
		}
		if (contract.Objective is not DeliveryObjective)
		{
			GameLog.Log("[Barks] Ignore: not a delivery.");
			return;
		}

		var pilotIds = run.PilotRegistry.Active
			.Where(pilot => _pilotHandles.ContainsKey(pilot.Id))
			.Select(pilot => pilot.Id).ToArray();
		Offer(pilotIds, DeliveryCompletionFlavors);
	}

	private void PlayOpeningBarks(State run)
	{
		foreach (var pilot in run.PilotRegistry.Active)
		{
			if (!Script(pilot.Id, PilotBarkCatalog.OpeningLines[0]))
				continue;

			foreach (var line in PilotBarkCatalog.OpeningLines.Skip(1))
				Script(pilot.Id, line);
			return;
		}
		GameLog.Log("[Barks] Ignore opener: no pilot accepted.");
	}

	/// <summary>Barks a random line of <paramref name="flavor"/> with probability <paramref name="chance"/>, if not on cooldown.</summary>
	public bool Offer(string pilotId, EBarkFlavor flavor, double chance = DefaultChance) =>
		Offer([pilotId], [flavor], chance);

	/// <summary>Rolls once for the offer, then chooses a pilot off cooldown, a flavor, and a line.</summary>
	public bool Offer(
		IReadOnlyList<string> pilotIds,
		IReadOnlyList<EBarkFlavor> flavors,
		double chance = DefaultChance)
	{
		ArgumentNullException.ThrowIfNull(pilotIds);
		ArgumentNullException.ThrowIfNull(flavors);
		if (flavors.Count == 0)
			throw new ArgumentException("At least one bark flavor is required.", nameof(flavors));
		foreach (var pilotId in pilotIds)
			ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentOutOfRangeException.ThrowIfNegative(chance);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(chance, 1.0);
		if (_now - _lastBarkAt < _globalGapSeconds)
		{
			GameLog.Log("[Barks] Ignore: global cooldown.");
			return false;
		}
		if (pilotIds.Count == 0)
		{
			GameLog.Log("[Barks] Ignore: no candidate pilots.");
			return false;
		}

		var eligiblePilots = pilotIds.Distinct(StringComparer.Ordinal)
			.Where(pilotId => _pilotHandles.ContainsKey(pilotId) && !IsCoolingDown(pilotId)).ToArray();
		if (eligiblePilots.Length == 0)
		{
			GameLog.Log("[Barks] Ignore: no eligible pilots.");
			return false;
		}
		var roll = _rng.NextDouble();
		if (roll >= chance)
		{
			GameLog.Log("[Barks] Ignore: chance roll.");
			return false;
		}

		var speakerId = eligiblePilots[_rng.Next(eligiblePilots.Length)];
		var flavor = flavors[_rng.Next(flavors.Count)];
		var spoken = Speak(speakerId, PilotBarkCatalog.Roll(flavor, _rng));
		GameLog.Log(spoken
			? $"[Barks] Pick flavor={flavor}, pilot={speakerId}."
			: "[Barks] Ignore: speech handle unavailable or rejected.");
		return spoken;
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
		if (!_pilotHandles.TryGetValue(pilotId, out var say))
		{
			return false;
		}
		if (!say(line))
		{
			return false;
		}

		_lastBarkAt = _now;
		_pilotLastSpokeAt[pilotId] = _now;
		return true;
	}
}
