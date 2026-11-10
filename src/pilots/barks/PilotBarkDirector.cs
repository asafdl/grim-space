using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Units;
using State = GrimSpace.Run.State;
using GrimSpace.Battle.Effects;
using GrimSpace.Units.Enums;

namespace GrimSpace.Pilots.Barks;

/// <summary>
/// Subscribes to game events, evaluates bark context, and voices pilots through registered handles.
/// Times are real seconds advanced via <see cref="Tick"/>.
/// </summary>
public sealed class PilotBarkDirector : IDisposable
{
	public sealed record Decision(string PilotId, EBarkFlavor Flavor, string Text, bool StartsCooldown = true);

	private static readonly EBarkFlavor[] DeliveryCompletionFlavors =
		[EBarkFlavor.Entitled, EBarkFlavor.Supportive];
	private static readonly EBarkFlavor[] EngagementDecisionFlavors =
		[EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty, EBarkFlavor.Analytical, EBarkFlavor.Impatient];
	private static readonly EBarkFlavor[] TravelFlavors = [EBarkFlavor.Impatient, EBarkFlavor.Entitled];
	private static readonly EBarkFlavor[] WreckAmbushFlavors =
		[EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty, EBarkFlavor.Analytical];
	private static readonly EBarkFlavor[] WreckSalvageFlavors = [EBarkFlavor.Supportive, EBarkFlavor.Impatient];
	private static readonly EBarkFlavor[] PlayerHitFlavors =
		[EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty, EBarkFlavor.Analytical, EBarkFlavor.Entitled];
	private static readonly EBarkFlavor[] EnemyKilledFlavors =
		[EBarkFlavor.Bloodthirsty, EBarkFlavor.Analytical, EBarkFlavor.Entitled, EBarkFlavor.Supportive];
	private static readonly EBarkFlavor[] AllyDestroyedFlavors =
		[EBarkFlavor.Anxious, EBarkFlavor.Bloodthirsty, EBarkFlavor.Entitled, EBarkFlavor.Supportive];

	public const double DefaultChance = 0.4;
	public const double BattleChance = 0.2;
	public const double DefaultGlobalGapSeconds = 8.0;
	public const double DefaultPilotCooldownSeconds = 20.0;

	private readonly Dictionary<string, Func<string, bool>> _pilotHandles = new(StringComparer.Ordinal);
	private readonly Dictionary<string, PilotPersonality> _pinnedPersonalities = new(StringComparer.Ordinal);
	private readonly State? _run;
	private readonly Func<string, PilotPersonality> _personalityReader;
	private IDisposable? _deliverySubscription;
	private IDisposable? _engagementSubscription;
	private IDisposable? _moveSubscription;
	private IDisposable? _wreckSubscription;
	private (long JourneyId, int DueTick, int EndTick)? _travelBark;
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
		double pilotCooldownSeconds = DefaultPilotCooldownSeconds,
		Func<string, PilotPersonality>? personalityReader = null)
	{
		ArgumentNullException.ThrowIfNull(rng);
		ArgumentOutOfRangeException.ThrowIfNegative(globalGapSeconds);
		ArgumentOutOfRangeException.ThrowIfNegative(pilotCooldownSeconds);
		_rng = rng;
		// Standalone dialogue previews have no roster and intentionally use neutral personalities.
		_personalityReader = personalityReader ?? (_ => default);
		_globalGapSeconds = globalGapSeconds;
		_pilotCooldownSeconds = pilotCooldownSeconds;
	}

	public PilotBarkDirector(State run, Random rng) : this(rng)
	{
		ArgumentNullException.ThrowIfNull(run);
		_run = run;
		_personalityReader = pilotId =>
		{
			if (_pinnedPersonalities.TryGetValue(pilotId, out var pinned))
				return pinned;
			return run.PilotRegistry.TryGet(pilotId, out var pilot)
				? pilot.EffectivePersonality
				: throw new KeyNotFoundException($"Bark pilot '{pilotId}' is not registered in the run.");
		};
		BindStarSystem();
	}

	internal void BindStarSystem()
	{
		if (_run is null)
			throw new InvalidOperationException("Cannot subscribe without a bound run.");
		_deliverySubscription?.Dispose();
		_engagementSubscription?.Dispose();
		_moveSubscription?.Dispose();
		_wreckSubscription?.Dispose();
		_travelBark = null;
		_deliverySubscription = _run.StarSystem
			.Subscribe<Record<ContractStateChanged>>(OnDeliveryContractStateChanged);
		_engagementSubscription = _run.StarSystem.Subscribe<ReachContactAction>(OnContactReached);
		_moveSubscription = _run.StarSystem.Subscribe<MoveAction>(OnMoved);
		_wreckSubscription = _run.StarSystem.Subscribe<ReachWreckageAction>(OnWreckReached);
	}

	public void RegisterPilot(string pilotId, Func<string, bool> say, PilotPersonality? personality = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentNullException.ThrowIfNull(say);
		_pilotHandles.Add(pilotId, say);
		if (personality is { } pinned)
			_pinnedPersonalities[pilotId] = pinned;
	}

	public void UnregisterPilot(string pilotId)
	{
		_pilotHandles.Remove(pilotId);
		_pinnedPersonalities.Remove(pilotId);
	}

	public void Dispose()
	{
		_deliverySubscription?.Dispose();
		_engagementSubscription?.Dispose();
		_moveSubscription?.Dispose();
		_wreckSubscription?.Dispose();
		_travelBark = null;
		_pilotHandles.Clear();
		_pinnedPersonalities.Clear();
	}

	public void Tick(double deltaSeconds)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(deltaSeconds);
		_now += deltaSeconds;
		CheckTravelBark();
		if (_run is null || _openingChecked || _pilotHandles.Count == 0)
			return;

		_openingChecked = true;
		if (_run.TryConsumeOpening())
			PlayOpeningBarks(_run);
	}

	private void OnMoved(MoveAction action)
	{
		if (_run is not { } run || action.UnitId != State.PlayerFleetUnitId)
			return;

		_travelBark = null;
		var fleet = run.StarSystem.Map.StateOf(State.PlayerFleetUnitId);
		if (fleet.Travel is not FleetTravel.Journey journey
			|| EngagementState.Phase(fleet) != EEngagementPhase.None)
			return;

		var duration = action.Path.DurationTicks(fleet.SpeedPerTick);
		if (duration < 2)
			return;

		_travelBark = (journey.Id, journey.StartTick + 1 + _rng.Next(duration - 1),
			journey.StartTick + duration);
	}

	private void CheckTravelBark()
	{
		if (_travelBark is not { } pending || _run is not { } run)
			return;

		var map = run.StarSystem.Map;
		if (!map.FleetRegistry.TryGet(State.PlayerFleetUnitId, out var fleet)
			|| fleet.State.Travel is not FleetTravel.Journey journey
			|| journey.Id != pending.JourneyId
			|| EngagementState.Phase(fleet.State) != EEngagementPhase.None
			|| run.ActiveBattle is not null
			|| map.Timeline.Clock.Current >= pending.EndTick)
		{
			_travelBark = null;
			GameLog.Log("[Barks] Ignore travel: journey ended or interrupted.");
			return;
		}
		if (map.Timeline.Clock.Current < pending.DueTick)
			return;

		_travelBark = null;
		OfferAssignedPilots(run, TravelFlavors, 1, 3);
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

		OfferAssignedPilots(run, DeliveryCompletionFlavors, 1, 1);
	}

	private void OnContactReached(ReachContactAction action)
	{
		if (_run is not { } run
			|| action.Intent != EContactIntent.Engagement
			|| (action.InitiatorId != State.PlayerFleetUnitId && action.TargetId != State.PlayerFleetUnitId)
			|| !EngagementQueries.TryGetPendingPlayerEngagement(
				run.StarSystem.Map, State.PlayerFleetUnitId, out _))
		{
			GameLog.Log("[Barks] Ignore: no player engagement decision.");
			return;
		}

		OfferAssignedPilots(run, EngagementDecisionFlavors, 1, 3);
	}

	private void OnWreckReached(ReachWreckageAction action)
	{
		if (_run is not { } run
			|| action.ActorId != State.PlayerFleetUnitId
			|| !run.StarSystem.Map.ContractRegistry.TryGet(action.ContractId, out var contract)
			|| contract.Objective is not WreckageObjective wreckage)
		{
			GameLog.Log("[Barks] Ignore: no matching player wreck contract.");
			return;
		}

		OfferAssignedPilots(
			run,
			wreckage.Outcome is WreckageOutcome.Ambush ? WreckAmbushFlavors : WreckSalvageFlavors,
			1,
			3);
	}

	private void OfferAssignedPilots(
		State run, IReadOnlyList<EBarkFlavor> flavors, int minPilots, int maxPilots)
	{
		var pilotIds = run.PilotRegistry.Assigned
			.Where(pilot => _pilotHandles.ContainsKey(pilot.Id))
			.Select(pilot => pilot.Id).ToArray();
		Offer(pilotIds, flavors, minPilots: minPilots, maxPilots: maxPilots);
	}

	private void PlayOpeningBarks(State run)
	{
		foreach (var pilot in run.PilotRegistry.Assigned)
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

	/// <summary>Rolls group size, then selects distinct pilots and rolls chance, flavor, and line per pilot.</summary>
	public bool Offer(
		IReadOnlyList<string> pilotIds,
		IReadOnlyList<EBarkFlavor> flavors,
		double chance = DefaultChance,
		int minPilots = 1,
		int maxPilots = 1)
	{
		var decisions = Select(pilotIds, flavors, chance, minPilots, maxPilots);
		var anySpoken = false;
		foreach (var decision in decisions)
			anySpoken |= Present(decision);
		return anySpoken;
	}

	public IReadOnlyList<Decision> SelectPlayerHit(
		ImpactFacts impact, ETeam targetTeam, IReadOnlyList<string> livingPilotIds)
	{
		if (targetTeam != ETeam.Player || impact.TotalDamage <= 0)
			return [];

		return Select(livingPilotIds, PlayerHitFlavors, BattleChance,
			minPilots: 1, maxPilots: 3, useCooldowns: false);
	}

	public IReadOnlyList<Decision> SelectEnemyKilled(
		ImpactFacts impact, ETeam targetTeam, IReadOnlyList<string> livingPilotIds)
	{
		if (targetTeam != ETeam.Enemy || !impact.IsLethal)
			return [];

		return Select(livingPilotIds, EnemyKilledFlavors, BattleChance,
			minPilots: 1, maxPilots: 3, useCooldowns: false);
	}

	public IReadOnlyList<Decision> SelectAllyDestroyed(
		ImpactFacts impact, ETeam targetTeam, IReadOnlyList<string> livingPilotIds)
	{
		if (targetTeam != ETeam.Player || !impact.IsLethal)
			return [];

		return Select(livingPilotIds, AllyDestroyedFlavors, BattleChance,
			minPilots: 1, maxPilots: 3, useCooldowns: false);
	}

	/// <summary>Chooses lines without displaying them or starting cooldowns; playback controls when to present them.</summary>
	public IReadOnlyList<Decision> Select(
		IReadOnlyList<string> pilotIds,
		IReadOnlyList<EBarkFlavor> flavors,
		double chance = DefaultChance,
		int minPilots = 1,
		int maxPilots = 1,
		bool useCooldowns = true)
	{
		ArgumentNullException.ThrowIfNull(pilotIds);
		ArgumentNullException.ThrowIfNull(flavors);
		if (flavors.Count == 0)
			throw new ArgumentException("At least one bark flavor is required.", nameof(flavors));
		foreach (var pilotId in pilotIds)
			ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentOutOfRangeException.ThrowIfNegative(chance);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(chance, 1.0);
		ArgumentOutOfRangeException.ThrowIfLessThan(minPilots, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(maxPilots, minPilots);
		if (useCooldowns && _now - _lastBarkAt < _globalGapSeconds)
		{
			GameLog.Log("[Barks] Ignore: global cooldown.");
			return [];
		}
		if (pilotIds.Count == 0)
		{
			GameLog.Log("[Barks] Ignore: no candidate pilots.");
			return [];
		}

		var eligiblePilots = pilotIds.Distinct(StringComparer.Ordinal)
			.Where(pilotId => _pilotHandles.ContainsKey(pilotId)
				&& (!useCooldowns || !IsCoolingDown(pilotId))).ToArray();
		if (eligiblePilots.Length == 0)
		{
			GameLog.Log("[Barks] Ignore: no eligible pilots.");
			return [];
		}
		var minCount = System.Math.Min(minPilots, eligiblePilots.Length);
		var maxCount = System.Math.Min(maxPilots, eligiblePilots.Length);
		var count = minCount + _rng.Next(maxCount - minCount + 1);
		var remaining = eligiblePilots.ToList();
		var decisions = new List<Decision>(count);
		for (var i = 0; i < count; i++)
		{
			var index = _rng.Next(remaining.Count);
			var speakerId = remaining[index];
			remaining.RemoveAt(index);
			if (_rng.NextDouble() >= chance)
			{
				GameLog.Log($"[Barks] Ignore: chance roll, pilot={speakerId}.");
				continue;
			}
			var flavorIndex = _personalityReader(speakerId)
				.SelectResponseIndex(flavors, PilotBarkCatalog.PersonalityAffinity, _rng);
			if (flavorIndex is null)
			{
				GameLog.Log($"[Barks] Ignore: no personality-compatible flavor, pilot={speakerId}.");
				continue;
			}
			var flavor = flavors[flavorIndex.Value];
			decisions.Add(new Decision(speakerId, flavor, PilotBarkCatalog.Roll(flavor, _rng), useCooldowns));
			GameLog.Log($"[Barks] Pick flavor={flavor}, pilot={speakerId}.");
		}
		return decisions;
	}

	/// <summary>Presents a selected line without rerolling or rechecking group cooldowns.</summary>
	public bool Present(Decision decision)
	{
		ArgumentNullException.ThrowIfNull(decision);
		ArgumentException.ThrowIfNullOrEmpty(decision.PilotId);
		ArgumentException.ThrowIfNullOrWhiteSpace(decision.Text);
		var spoken = Speak(decision.PilotId, decision.Text, decision.StartsCooldown);
		if (!spoken)
			GameLog.Log("[Barks] Ignore: speech handle unavailable or rejected.");
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

	private bool Speak(string pilotId, string line, bool startsCooldown = true)
	{
		if (!_pilotHandles.TryGetValue(pilotId, out var say))
		{
			return false;
		}
		if (!say(line))
		{
			return false;
		}

		if (startsCooldown)
		{
			_lastBarkAt = _now;
			_pilotLastSpokeAt[pilotId] = _now;
		}
		return true;
	}
}
