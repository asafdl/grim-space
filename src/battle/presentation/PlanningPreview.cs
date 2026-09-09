using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
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
	private const string PreviewPatrolId = "__preview_patrol__";
	private const string PreviewTorpedoId = "__preview_torpedo__";

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

	public WeaponPeek Weapons(BattleSimulation sim, string actorId)
	{
		var world = sim.World;
		var runtime = sim.RuntimeFor(actorId);
		var torpedoMounts = new HashSet<ESpatialOrientation>();
		foreach (var mountedOn in TorpedoMountedDirections)
		{
			var action = new TorpedoAction(actorId, mountedOn, PreviewTorpedoId);
			if (TorpedoDef.Instance.IsLegal(action, world, runtime))
				torpedoMounts.Add(mountedOn);
		}

		return new WeaponPeek(
			FlakDef.Instance.IsLegal(
				new FlakAction(actorId, ESpatialOrientation.Port),
				world,
				runtime),
			FlakDef.Instance.IsLegal(
				new FlakAction(actorId, ESpatialOrientation.Starboard),
				world,
				runtime),
			RailgunDef.Instance.IsLegal(new RailgunAction(actorId), world, runtime),
			torpedoMounts);
	}

	public AbilityLegality Abilities(BattleSimulation sim, string actorId)
	{
		var world = sim.World;
		var runtime = sim.RuntimeFor(actorId);
		return new AbilityLegality(
			Weapons(sim, actorId),
			SpawnPatrolDef.Instance.IsLegal(
				new SpawnPatrolAction(actorId, PreviewPatrolId),
				world,
				runtime),
			DetonateDef.Instance.IsLegal(new DetonateAction(actorId), world, runtime));
	}

	public QueuedWeaponState QueuedWeapon(BattleSimulation sim, string playerId)
	{
		ESpatialOrientation? flakMountedOn = null;
		UnitDisplayState? flakActorState = null;
		var railgun = false;
		UnitDisplayState? railgunActorState = null;
		ESpatialOrientation? torpedoMountedOn = null;
		UnitDisplayState? torpedoActorState = null;

		for (var i = sim.Actions.Count - 1; i >= 0; i--)
		{
			if (sim.Actions[i].ActorId != playerId)
				continue;

			switch (sim.Actions[i])
			{
				case FlakAction flak when flakMountedOn is null:
					flakMountedOn = flak.MountedOn;
					flakActorState = ActorStateAt(sim, playerId, i);
					break;
				case RailgunAction when !railgun:
					railgun = true;
					railgunActorState = ActorStateAt(sim, playerId, i);
					break;
				case TorpedoAction torpedo when torpedoMountedOn is null:
					torpedoMountedOn = torpedo.MountedOn;
					torpedoActorState = ActorStateAt(sim, playerId, i);
					break;
			}
		}

		return new QueuedWeaponState
		{
			FlakMountedOn = flakMountedOn,
			FlakActorStateAtQueue = flakActorState,
			Railgun = railgun,
			RailgunActorStateAtQueue = railgunActorState,
			TorpedoMountedOn = torpedoMountedOn,
			TorpedoActorStateAtQueue = torpedoActorState,
		};
	}

	public HashSet<string> ThreatenedUnitIds(
		BattleSimulation sim,
		string playerId,
		InteractionState state)
	{
		var targets = new HashSet<string>();

		if (state.Mode == EPlayerMode.Flak)
		{
			if (state.StagedMountedOn is ESpatialOrientation stagedMountedOn)
				targets.UnionWith(ImpactTargets(sim.Peek(new FlakAction(playerId, stagedMountedOn))));
			else if (state.FlakHoverMountedOn is ESpatialOrientation hoverMountedOn)
				targets.UnionWith(ImpactTargets(sim.Peek(new FlakAction(playerId, hoverMountedOn))));
		}

		if (state.RailgunHovered)
			targets.UnionWith(ImpactTargets(sim.Peek(new RailgunAction(playerId))));

		for (var i = 0; i < sim.Actions.Count; i++)
		{
			if (sim.Actions[i].ActorId != playerId)
				continue;

			switch (sim.Actions[i])
			{
				case FlakAction:
				case RailgunAction:
					targets.UnionWith(ImpactTargets(sim.RecordsFor(i)));
					break;
			}
		}

		return targets;
	}

	public IReadOnlyList<IReadOnlySet<Coord>> TorpedoEnvelopeLayers(
		BattleSimulation sim,
		string playerId,
		InteractionState state)
	{
		EnsureSim(sim);
		var queued = QueuedWeapon(sim, playerId);

		if (state.Mode == EPlayerMode.Torpedo && state.StagedMountedOn is ESpatialOrientation staged)
			return EnvelopeLayersForMount(sim, playerId, staged);

		if (state.Mode == EPlayerMode.Torpedo && state.TorpedoHoverMountedOn is ESpatialOrientation hover)
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

		var peek = sim.Peek(new TorpedoAction(playerId, mountedOn, PreviewTorpedoId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(PreviewTorpedoId, out var spawned))
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
		var cacheKey =
			$"queued|{sim.WorldVersion}|{MovePreviewCache.PrefixKey(sim.Actions)}|{queued.MountedOn}|{queued.SpawnedUnitId}";
		if (_envelopeCacheKey == cacheKey)
			return _envelopeCache;

		var peek = sim.Peek(EndOfPhaseDef.Instance.Bind(playerId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(queued.SpawnedUnitId, out var spawned))
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
			.ToDictionary(unit => unit.State.Id, unit => UnitDisplayState.Capture(unit.State));

	private static UnitDisplayState ActorStateAt(BattleSimulation sim, string playerId, int actionIndex)
	{
		var world = sim.ReplayWorld(actionIndex);
		return UnitDisplayState.Capture(UnitRegistry.For(world).UnitOf(playerId).State);
	}

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
