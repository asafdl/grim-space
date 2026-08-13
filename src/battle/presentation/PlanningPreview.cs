using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Sim-derived planning previews for presentation. Hover-sensitive fields peek the planning sim locally.
/// </summary>
public sealed class PlanningPreview
{
	private readonly MovePreviewCache _moveCache = new();

	private BattleSimulation? _lastSim;
	private string? _envelopeCacheKey;
	private IReadOnlyList<IReadOnlySet<Coord>> _envelopeCache = [];

	public IReadOnlyDictionary<string, UnitDisplayState> PreviewUnits(
		BattleSimulation sim,
		string playerId) =>
		CaptureUnits(PreviewWorld(sim, playerId));

	public IReadOnlyList<MovePathOption> MoveOptions(
		BattleSimulation sim,
		string playerId,
		string focusId,
		bool isPlanning)
	{
		if (!isPlanning)
			return [];

		EnsureSim(sim);
		var moveActorId = focusId;
		var inspecting = moveActorId != playerId;
		var movePathApBaseline = sim.RuntimeFor(moveActorId).ActivePath?.PathApSpent ?? 0;
		var previewWorld = PreviewWorld(sim, playerId);

		if (moveActorId == playerId)
		{
			return _moveCache.GetPaths(sim, playerId, sim.Actions)
				.Select(path => new MovePathOption(
					path.Cells,
					path.EndPosition,
					path.ExtensionApCost(movePathApBaseline),
					path.Steps.Select(step => step.Direction).ToList()))
				.ToList();
		}

		if (!inspecting || !CaptureUnits(previewWorld).ContainsKey(moveActorId))
			return [];

		return MovePathEndpoints.DiscoverExtensions(sim, moveActorId)
			.Select(path => new MovePathOption(
				path.Cells,
				path.EndPosition,
				path.ExtensionApCost(movePathApBaseline),
				path.Steps.Select(step => step.Direction).ToList()))
			.ToList();
	}

	public int MovePathApBaseline(BattleSimulation sim, string playerId, string focusId) =>
		sim.RuntimeFor(focusId).ActivePath?.PathApSpent ?? 0;

	public IReadOnlyList<Coord> CommittedMovePath(BattleSimulation sim, string playerId) =>
		sim.RuntimeFor(playerId).ActivePath?.Cells ?? [];

	public WeaponPeek Weapons(BattleSimulation sim, string playerId)
	{
		var torpedoMounts = new HashSet<ESpatialOrientation>();
		foreach (var mountedOn in TorpedoMountedDirections)
		{
			if (sim.Peek(new TorpedoAction(playerId, mountedOn)) is not null)
				torpedoMounts.Add(mountedOn);
		}

		return new WeaponPeek(
			sim.Peek(new FlakAction(playerId, ESpatialOrientation.Port)) is not null,
			sim.Peek(new FlakAction(playerId, ESpatialOrientation.Starboard)) is not null,
			sim.Peek(new RailgunAction(playerId)) is not null,
			torpedoMounts);
	}

	public QueuedWeaponState QueuedWeapon(BattleSimulation sim, string playerId)
	{
		ESpatialOrientation? queuedFlak = null;
		var queuedRailgun = false;
		ESpatialOrientation? queuedTorpedo = null;
		for (var i = sim.Actions.Count - 1; i >= 0; i--)
		{
			if (sim.Actions[i].ActorId != playerId)
				continue;

			switch (sim.Actions[i])
			{
				case FlakAction flak:
					queuedFlak = flak.MountedOn;
					break;
				case RailgunAction:
					queuedRailgun = true;
					break;
				case TorpedoAction torpedo:
					queuedTorpedo = torpedo.MountedOn;
					break;
				default:
					continue;
			}

			break;
		}

		return new QueuedWeaponState
		{
			FlakMountedOn = queuedFlak,
			Railgun = queuedRailgun,
			TorpedoMountedOn = queuedTorpedo,
		};
	}

	public HashSet<string> ThreatenedUnitIds(
		BattleSimulation sim,
		string playerId,
		InteractionState state)
	{
		if (state.FlakHoverMountedOn is ESpatialOrientation hoverMountedOn)
			return ImpactTargets(sim.Peek(new FlakAction(playerId, hoverMountedOn)));

		if (state.RailgunHovered)
			return ImpactTargets(sim.Peek(new RailgunAction(playerId)));

		for (var i = sim.Actions.Count - 1; i >= 0; i--)
		{
			if (sim.Actions[i].ActorId != playerId)
				continue;

			switch (sim.Actions[i])
			{
				case FlakAction:
				case RailgunAction:
					return ImpactTargets(sim.RecordsFor(i));
				case TorpedoAction:
					return [];
				default:
					continue;
			}
		}

		return [];
	}

	public IReadOnlyList<IReadOnlySet<Coord>> TorpedoEnvelopeLayers(
		BattleSimulation sim,
		string playerId,
		InteractionState state)
	{
		EnsureSim(sim);
		var queued = QueuedWeapon(sim, playerId);

		if (state.TorpedoHoverMountedOn is ESpatialOrientation hover)
			return EnvelopeLayersForMount(sim, playerId, hover);

		if (queued.TorpedoMountedOn is not ESpatialOrientation queuedMountedOn)
			return [];

		for (var i = sim.Actions.Count - 1; i >= 0; i--)
		{
			if (sim.Actions[i] is TorpedoAction torpedo
				&& torpedo.ActorId == playerId
				&& torpedo.MountedOn == queuedMountedOn)
				return EnvelopeLayersForQueued(sim, playerId, torpedo);
		}

		return [];
	}

	private void EnsureSim(BattleSimulation sim)
	{
		if (ReferenceEquals(sim, _lastSim))
			return;

		ClearCaches();
		_lastSim = sim;
	}

	public void ClearCaches()
	{
		_moveCache.Clear();
		_envelopeCacheKey = null;
		_envelopeCache = [];
		_lastSim = null;
	}

	private static readonly ESpatialOrientation[] TorpedoMountedDirections =
	[
		ESpatialOrientation.Retro,
		ESpatialOrientation.Ventral,
		ESpatialOrientation.Dorsal,
	];

	private IReadOnlyList<IReadOnlySet<Coord>> EnvelopeLayersForMount(
		BattleSimulation sim,
		string playerId,
		ESpatialOrientation mountedOn)
	{
		var cacheKey =
			$"hover|{sim.WorldVersion}|{MovePreviewCache.PrefixKey(sim.Actions)}|{mountedOn}";
		if (_envelopeCacheKey == cacheKey)
			return _envelopeCache;

		var spawnedId = sim.World.IdRegistry.NextUnitId(EType.Torpedo);
		var peek = sim.Peek(new TorpedoAction(playerId, mountedOn, spawnedId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(spawnedId, out var spawned))
		{
			_envelopeCacheKey = cacheKey;
			_envelopeCache = [];
			return _envelopeCache;
		}

		var session = new BattleSimulation(peek.Value.World, peek.Value.Runtimes);
		session.Begin(sim.AnchorTick, sim.WorldVersion);
		_envelopeCacheKey = cacheKey;
		_envelopeCache = TorpedoReachEnvelope.Build(session, spawned.State.Id).Layers;
		return _envelopeCache;
	}

	private IReadOnlyList<IReadOnlySet<Coord>> EnvelopeLayersForQueued(
		BattleSimulation sim,
		string playerId,
		TorpedoAction queued)
	{
		if (queued.SpawnedUnitId is not { } spawnedId)
			return [];

		var cacheKey =
			$"queued|{sim.WorldVersion}|{MovePreviewCache.PrefixKey(sim.Actions)}|{queued.MountedOn}|{spawnedId}";
		if (_envelopeCacheKey == cacheKey)
			return _envelopeCache;

		var peek = sim.Peek(EndOfPhaseDef.Instance.Bind(playerId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(spawnedId, out var spawned))
		{
			_envelopeCacheKey = cacheKey;
			_envelopeCache = [];
			return _envelopeCache;
		}

		var session = new BattleSimulation(peek.Value.World, peek.Value.Runtimes);
		session.Begin(sim.AnchorTick, sim.WorldVersion);
		_envelopeCacheKey = cacheKey;
		_envelopeCache = TorpedoReachEnvelope.Build(session, spawned.State.Id).Layers;
		return _envelopeCache;
	}

	private static BattleWorld PreviewWorld(BattleSimulation sim, string playerId)
	{
		var peek = sim.Peek(EndOfPhaseDef.Instance.Bind(playerId));
		return peek?.World ?? sim.World;
	}

	private static Dictionary<string, UnitDisplayState> CaptureUnits(BattleWorld world) =>
		UnitRegistry.For(world).All
			.Where(unit => unit.State.IsAlive)
			.ToDictionary(unit => unit.State.Id, unit => UnitDisplayState.Capture(unit.State));

	private static HashSet<string> ImpactTargets(PeekFrame<BattleWorld, ActorRuntime>? peek) =>
		peek is { } frame ? ImpactTargets(frame.Records) : [];

	private static HashSet<string> ImpactTargets(IReadOnlyList<IRecord> records)
	{
		var targets = new HashSet<string>();
		foreach (var record in records)
		{
			if (record is Record<ImpactFacts> { Value.TargetId: var targetId })
				targets.Add(targetId);
		}

		return targets;
	}
}
