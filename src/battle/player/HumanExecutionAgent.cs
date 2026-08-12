using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Player;

public sealed class HumanExecutionAgent
	: IExecutionAgent<BattleWorld, ActorRuntime, Unit>,
		IHumanActionSink
{
	private readonly MovePreviewCache _moveCache = new();

	private string _humanActorId = string.Empty;
	private int _turnNumber;
	private bool _canAct;
	private IReadOnlySet<Coord> _hazardCells = new HashSet<Coord>();

	private string? _envelopeCacheKey;
	private IReadOnlyList<IReadOnlySet<Coord>> _envelopeCache = [];

	public BattleSimulation Sim { get; private set; } = null!;

	public HumanTurnSnapshot Current { get; private set; } = HumanTurnSnapshot.Empty(string.Empty);

	public event Action<HumanTurnSnapshot>? Changed;

	public void Init(Func<BattleSimulation> createSim, HumanTurnContext context)
	{
		_humanActorId = context.HumanActorId;
		_turnNumber = context.TurnNumber;
		_canAct = context.CanAct;
		_hazardCells = context.HazardCells;
		ClearCaches();
		Sim = createSim();
		PublishSnapshot();
	}

	public Task<IReadOnlyList<IAction>> GetActionsAsync(
		Unit actor,
		Func<BattleSimulation> createSim)
	{
		if (!Sim.TryCommit(out var actions, out _))
			return Task.FromResult<IReadOnlyList<IAction>>([]);

		IReadOnlyList<IAction> streamlined =
			OrientationStreamline.Compact(actions, Sim.UndoGroups);
		return Task.FromResult(streamlined);
	}

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (!_canAct || actions.Count == 0)
			return false;

		if (!Sim.TryEnqueue(keepRecords: true, actions: [..actions]))
			return false;

		if (actions.Any(action => action is HeadingTurnAction or RollAction))
			OrientationStreamline.CompactQueue(Sim);

		PublishSnapshot();
		return true;
	}

	public bool Undo()
	{
		if (!_canAct || Sim.Actions.Count == 0)
			return false;

		if (!Sim.TryUndoLast())
			return false;

		PublishSnapshot();
		return true;
	}

	public bool Commit()
	{
		if (!_canAct)
			return false;

		return Sim.TryCommit(out _, out _);
	}

	public HumanTurnSnapshot BuildSnapshot(HumanTurnViewInput view = default) =>
		BuildSnapshotCore(view);

	private void ClearCaches()
	{
		_moveCache.Clear();
		_envelopeCacheKey = null;
		_envelopeCache = [];
	}

	private void PublishSnapshot()
	{
		Current = BuildSnapshotCore();
		Changed?.Invoke(Current);
	}

	private HumanTurnSnapshot BuildSnapshotCore(HumanTurnViewInput view = default)
	{
		var previewWorld = GetPreviewWorld();
		var canAct = _canAct;
		var moveActorId = view.PresentationFocusId ?? _humanActorId;
		var inspecting = moveActorId != _humanActorId;
		var movePathApBaseline = Sim.RuntimeFor(moveActorId).ActivePath?.PathApSpent ?? 0;

		IReadOnlyList<MovePathOption> moveOptions = [];
		if (canAct && moveActorId == _humanActorId)
		{
			moveOptions = _moveCache.GetPaths(Sim, _humanActorId, Sim.Actions)
				.Select(path => new MovePathOption(
					path.Cells,
					path.EndPosition,
					path.ExtensionApCost(movePathApBaseline),
					path.Steps.Select(step => step.Direction).ToList()))
				.ToList();
		}
		else if (inspecting && CaptureUnits(previewWorld).ContainsKey(moveActorId))
		{
			moveOptions = MovePathEndpoints.DiscoverExtensions(Sim, moveActorId)
				.Select(path => new MovePathOption(
					path.Cells,
					path.EndPosition,
					path.ExtensionApCost(movePathApBaseline),
					path.Steps.Select(step => step.Direction).ToList()))
				.ToList();
		}

		var queuedWeapon = canAct ? BuildQueuedWeapon() : QueuedWeaponState.Empty;
		var weapons = canAct ? BuildWeapons() : WeaponPeek.Empty;
		var torpedoEnvelopeLayers = canAct
			? GetTorpedoEnvelopeLayers(view, queuedWeapon)
			: [];
		var threatenedUnitIds = canAct
			? ThreatenedUnitIds(view)
			: new HashSet<string>();

		return new HumanTurnSnapshot
		{
			HumanActorId = _humanActorId,
			TurnNumber = _turnNumber,
			CanAct = canAct,
			Units = CaptureUnits(Sim.World),
			PreviewUnits = CaptureUnits(previewWorld),
			HazardCells = _hazardCells,
			MoveOptions = moveOptions,
			CommittedMovePath = Sim.RuntimeFor(_humanActorId).ActivePath?.Cells ?? [],
			QueuedWeapon = queuedWeapon,
			Weapons = weapons,
			TorpedoEnvelopeLayers = torpedoEnvelopeLayers,
			ThreatenedUnitIds = threatenedUnitIds,
			CanUndo = canAct && Sim.Actions.Count > 0,
			CanCommit = canAct,
			MovePathApBaseline = movePathApBaseline,
		};
	}

	private WeaponPeek BuildWeapons()
	{
		var torpedoMounts = TorpedoConfig.EnabledMounts
			.Where(mount => Sim.Peek(new TorpedoAction(_humanActorId, mount)) is not null)
			.ToHashSet();

		return new WeaponPeek(
			Sim.Peek(new FlakAction(_humanActorId, EFlakMount.Port)) is not null,
			Sim.Peek(new FlakAction(_humanActorId, EFlakMount.Starboard)) is not null,
			Sim.Peek(new RailgunAction(_humanActorId)) is not null,
			torpedoMounts);
	}

	private BattleWorld GetPreviewWorld()
	{
		var peek = Sim.Peek(EndOfPhaseDef.Instance.Bind(_humanActorId));
		return peek?.World ?? Sim.World;
	}

	private static Dictionary<string, UnitDisplayState> CaptureUnits(BattleWorld world) =>
		UnitRegistry.For(world).All
			.Where(unit => unit.State.IsAlive)
			.ToDictionary(unit => unit.State.Id, unit => UnitDisplayState.Capture(unit.State));

	private QueuedWeaponState BuildQueuedWeapon()
	{
		EFlakMount? queuedFlak = null;
		var queuedRailgun = false;
		ETorpedoMount? queuedTorpedo = null;
		for (var i = Sim.Actions.Count - 1; i >= 0; i--)
		{
			if (Sim.Actions[i].ActorId != _humanActorId)
				continue;

			switch (Sim.Actions[i])
			{
				case FlakAction flak:
					queuedFlak = flak.Mount;
					break;
				case RailgunAction:
					queuedRailgun = true;
					break;
				case TorpedoAction torpedo:
					queuedTorpedo = torpedo.Mount;
					break;
				default:
					continue;
			}

			break;
		}

		return new QueuedWeaponState
		{
			FlakMount = queuedFlak,
			Railgun = queuedRailgun,
			TorpedoMount = queuedTorpedo,
		};
	}

	private HashSet<string> ThreatenedUnitIds(HumanTurnViewInput view)
	{
		if (view.FlakHoverMount is EFlakMount hoverMount)
			return ImpactTargets(Sim.Peek(new FlakAction(_humanActorId, hoverMount)));

		if (view.RailgunHovered)
			return ImpactTargets(Sim.Peek(new RailgunAction(_humanActorId)));

		for (var i = Sim.Actions.Count - 1; i >= 0; i--)
		{
			if (Sim.Actions[i].ActorId != _humanActorId)
				continue;

			switch (Sim.Actions[i])
			{
				case FlakAction:
				case RailgunAction:
					return ImpactTargets(Sim.RecordsFor(i));
				case TorpedoAction:
					return [];
				default:
					continue;
			}
		}

		return [];
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

	private IReadOnlyList<IReadOnlySet<Coord>> GetTorpedoEnvelopeLayers(
		HumanTurnViewInput view,
		QueuedWeaponState queued)
	{
		if (view.TorpedoHoverMount is ETorpedoMount hover)
			return GetEnvelopeLayersForMount(hover);

		if (queued.TorpedoMount is not ETorpedoMount queuedMount)
			return [];

		for (var i = Sim.Actions.Count - 1; i >= 0; i--)
		{
			if (Sim.Actions[i] is TorpedoAction torpedo
				&& torpedo.ActorId == _humanActorId
				&& torpedo.Mount == queuedMount)
				return GetEnvelopeLayersForQueued(torpedo);
		}

		return [];
	}

	private IReadOnlyList<IReadOnlySet<Coord>> GetEnvelopeLayersForMount(ETorpedoMount mount)
	{
		var cacheKey =
			$"hover|{Sim.WorldVersion}|{MovePreviewCache.PrefixKey(Sim.Actions)}|{mount}";
		if (_envelopeCacheKey == cacheKey)
			return _envelopeCache;

		var spawnedId = Sim.World.IdRegistry.NextUnitId(EType.Torpedo);
		var peek = Sim.Peek(new TorpedoAction(_humanActorId, mount, spawnedId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(spawnedId, out var spawned))
		{
			_envelopeCacheKey = cacheKey;
			_envelopeCache = [];
			return _envelopeCache;
		}

		var session = new BattleSimulation(peek.Value.World, peek.Value.Runtimes);
		session.Begin(Sim.AnchorTick, Sim.WorldVersion);
		_envelopeCacheKey = cacheKey;
		_envelopeCache = TorpedoReachEnvelope.Build(session, spawned.State.Id).Layers;
		return _envelopeCache;
	}

	private IReadOnlyList<IReadOnlySet<Coord>> GetEnvelopeLayersForQueued(TorpedoAction queued)
	{
		if (queued.SpawnedUnitId is not { } spawnedId)
			return [];

		var cacheKey =
			$"queued|{Sim.WorldVersion}|{MovePreviewCache.PrefixKey(Sim.Actions)}|{queued.Mount}|{spawnedId}";
		if (_envelopeCacheKey == cacheKey)
			return _envelopeCache;

		var peek = Sim.Peek(EndOfPhaseDef.Instance.Bind(_humanActorId));
		if (peek is null
			|| !UnitRegistry.For(peek.Value.World).TryGet(spawnedId, out var spawned))
		{
			_envelopeCacheKey = cacheKey;
			_envelopeCache = [];
			return _envelopeCache;
		}

		var session = new BattleSimulation(peek.Value.World, peek.Value.Runtimes);
		session.Begin(Sim.AnchorTick, Sim.WorldVersion);
		_envelopeCacheKey = cacheKey;
		_envelopeCache = TorpedoReachEnvelope.Build(session, spawned.State.Id).Layers;
		return _envelopeCache;
	}
}
