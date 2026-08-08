using System.Diagnostics;
using GrimSpace.Core.Log;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle;

public sealed class BattleOrchestrator
{
	private readonly Engine<BattleWorld, ActorRuntime> _engine;
	private readonly string _opponentId;
	private readonly HumanExecutionAgent _playerAgent;

	private bool _resolveInProgress;

	internal BattleOrchestrator(
		Engine<BattleWorld, ActorRuntime> engine,
		BattleLayout layout,
		string playerId,
		string opponentId,
		HumanExecutionAgent playerAgent)
	{
		_engine = engine;
		Layout = layout;
		PlayerId = playerId;
		_opponentId = opponentId;
		_playerAgent = playerAgent;
	}

	internal Engine<BattleWorld, ActorRuntime> Engine => _engine;

	public BattleLayout Layout { get; }
	public BattleSimulation Sim => _playerAgent.Sim;
	public string PlayerId { get; }
	public string OpponentId => _opponentId;
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }
	public int TurnNumber => _engine.Tick;
	public string? ActiveUnitId { get; private set; }

	public static BattleOrchestrator FromEncounter(Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var timeline = new Timeline();
		var nonUnits = new Dictionary<string, NonUnit>();
		var ids = new UnitIdRegistry();

		foreach (var spawn in encounter.WorldHazards)
		{
			var hazard = Hazard.Asteroid(
				ids.NextNonUnitId("asteroid"),
				spawn.Center,
				grid,
				spawn.Radius,
				spawn.VisualId);
			nonUnits[hazard.Id] = hazard;
		}

		var terrainHazards = nonUnits.Values.OfType<Hazard>().ToList();
		var blockedCells = BattleWorld.TerrainBlockedCells(terrainHazards);

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(
				spawn.Unit,
				spawn.Position,
				ids,
				spawn.InitialMomentum,
				spawn.Fore,
				spawn.Dorsal))
			.ToArray();

		var player = units.First(unit => unit.Alliance.Team == ETeam.Player);
		var opponentId = units.First(unit => unit.Alliance.Team == ETeam.Enemy).State.Id;
		var world = BattleWorld.FromLive(units, nonUnits, grid, blockedCells, timeline);
		var layout = BattleLayout.FromEncounter(grid, terrainHazards, units);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in units)
			actorRuntimes.For(unit.State.Id);
		actorRuntimes.For(EntityIds.System);

		var engine = new Engine<BattleWorld, ActorRuntime>(world, actorRuntimes);
		var playerAgent = (HumanExecutionAgent)player.ExecutionAgent;
		var orchestrator = new BattleOrchestrator(engine, layout, player.State.Id, opponentId, playerAgent);

		orchestrator.SetActiveUnit(player.State.Id);
		orchestrator.BeginTurn();
		return orchestrator;
	}

	public void SetActiveUnit(string unitId) => ActiveUnitId = unitId;

	public bool IsActive(string unitId) => ActiveUnitId == unitId;

	public void BeginTurn() => _playerAgent.BeginTurn(_engine.CreateSimulation);

	public bool CanAct(Unit unit) =>
		!IsBattleOver && IsActive(unit.State.Id) && unit.State.IsAlive;

	public Unit? GetActiveUnit()
	{
		if (ActiveUnitId is not string id
			|| !UnitRegistry.For(_engine.World).TryGet(id, out var unit))
		{
			return null;
		}

		return unit;
	}

	public TurnReplay ResolveTurn() =>
		ResolveTurnAsync().GetAwaiter().GetResult();

	public async Task<TurnReplay> ResolveTurnAsync()
	{
		if (IsBattleOver || _resolveInProgress)
			throw new InvalidOperationException("Cannot resolve turn while battle is over or already resolving.");

		_resolveInProgress = true;
		try
		{
			var replay = await ExecuteTurnAsync();
			var outcome = EvaluateBattleOutcome();
			IsBattleOver = outcome.IsOver;
			WinnerId = outcome.WinnerId;

			if (UnitRegistry.For(_engine.World).TryGet(PlayerId, out var player) && player.State.IsAlive)
				BeginTurn();

			return replay;
		}
		finally
		{
			_resolveInProgress = false;
		}
	}

	private async Task<TurnReplay> ExecuteTurnAsync()
	{
		var resolveTimer = Stopwatch.StartNew();
		var turnNumber = TurnNumber;
		var unitsAtTurnStart = SnapshotAll();
		var hazardsBeforeResolve = _engine.World.TurnHazards.ToList();
		IReadOnlyDictionary<string, UnitState>? unitsAfterPlayer = null;

		_engine.ActorRuntimes.Reset();

		var units = UnitRegistry.For(_engine.World);
		for (var node = units.First; node is not null; node = node.Next)
		{
			if (!units.TryGet(node.Value, out var live) || !live.State.IsAlive)
				continue;

			var planned = await live.ExecutionAgent.GetActionsAsync(live, _engine.CreateSimulation);
			CommitActor(live.State.Id, planned);

			if (live.State.Id == PlayerId)
				unitsAfterPlayer = SnapshotAll();
		}

		CommitRoundUpkeep();
		var batches = _engine.History();
		_engine.AdvanceTick();
		FinalizeRound();

		GameLog.Log(
			$"Turn {turnNumber} sim: "
			+ $"total={resolveTimer.Elapsed.TotalMilliseconds:F1}ms "
			+ $"batches={batches.Count}");

		var endStates = SnapshotAll();
		StateLog.LogTurnResolution(
			turnNumber,
			batches,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			unitsAfterPlayer ?? endStates,
			endStates);

		return new TurnReplay(unitsAtTurnStart, batches, endStates);
	}

	private IReadOnlyList<IAction> CommitActor(string actorId, IReadOnlyList<IAction> actions)
	{
		var batch = new List<IAction>(actions.Count + 1);
		batch.AddRange(actions);
		batch.Add(new EndOfPhaseAction(actorId));
		return _engine.Commit([..batch]);
	}

	private IReadOnlyList<IAction> CommitRoundUpkeep()
	{
		var batch = new List<IAction>();
		foreach (var unitId in UnitRegistry.For(_engine.World).Ids)
			batch.Add(new RoundUpkeepAction(unitId));
		batch.Add(new ClearTurnHazardsAction());
		return _engine.Commit([..batch]);
	}

	private void FinalizeRound()
	{
		if (UnitRegistry.For(_engine.World).TryGet(PlayerId, out var player) && player.State.IsAlive)
			SetActiveUnit(player.State.Id);
	}

	private Dictionary<string, UnitState> SnapshotAll() =>
		UnitRegistry.For(_engine.World).All.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());

	private BattleOutcome EvaluateBattleOutcome()
	{
		var units = UnitRegistry.For(_engine.World).All;
		var player = units.FirstOrDefault(unit => unit.Alliance.Team == ETeam.Player);
		var enemy = units.FirstOrDefault(unit => unit.Alliance.Team == ETeam.Enemy);

		if (enemy is not null && !enemy.State.IsAlive)
			return new BattleOutcome(true, player?.State.Id);

		if (player is not null && !player.State.IsAlive)
			return new BattleOutcome(true, enemy?.State.Id);

		return new BattleOutcome(false, null);
	}

	private readonly record struct BattleOutcome(bool IsOver, string? WinnerId);
}
