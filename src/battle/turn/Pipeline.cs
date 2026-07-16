using GrimSpace.Units.Enums;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle.Turn;

public sealed class Pipeline
{
	private readonly BoundedGrid _grid;
	private readonly IReadOnlyList<Unit> _units;
	private readonly Manager _turn;
	private readonly HazardSystem _hazards;
	private readonly TurnOrchestrator _orchestrator;

	public Pipeline(
		BoundedGrid grid,
		IReadOnlyList<Unit> units,
		Manager turn,
		HazardSystem hazards)
	{
		_grid = grid;
		_units = units;
		_turn = turn;
		_hazards = hazards;
		_orchestrator = new TurnOrchestrator(grid, units, hazards);
	}

	public PipelineResult Resolve(FinalizedPlan playerPlan, IPresentationEventSink? sink)
	{
		var turnNumber = _turn.TurnNumber;
		var unitsAtTurnStart = SnapshotAll();

		var commit = TurnCommit.Build(
			playerPlan,
			_units,
			_grid,
			_hazards.NonUnits,
			_hazards.GetOccupiedCells(),
			_hazards.GetBlockedCells());

		var execution = _orchestrator.Execute(commit, sink);
		var outcome = RulesEngine.Evaluate(_units);

		var hazardsBeforeResolve = _hazards.Active.ToList();
		if (!outcome.IsOver)
			_orchestrator.ExecuteEnvironmentPhase();

		_hazards.Clear();
		outcome = RulesEngine.Evaluate(_units);

		ExecuteUpkeepPhase();

		StateLog.LogTurnResolution(
			turnNumber,
			commit.PlayerPlan.BattleActions,
			commit.EnemyPlan.BattleActions,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			execution.UnitsAfterPlayer,
			SnapshotAll(),
			SnapshotAll());

		return new PipelineResult(outcome.IsOver, outcome.WinnerId);
	}

	private void ExecuteUpkeepPhase()
	{
		foreach (var unit in _units)
		{
			unit.State.ActionPoints = unit.State.Stats.MaxAp;
			unit.State.MissilesRemaining = unit.State.Stats.MissilesPerTurn;
		}

		_turn.AdvanceTurn();

		var player = _units.FirstOrDefault(unit => unit.Controller == EController.Player);
		if (player is not null)
			_turn.SetActiveUnit(player.State.Id);
	}

	private Dictionary<string, UnitState> SnapshotAll() =>
		_units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());
}

public readonly record struct PipelineResult(bool IsBattleOver, string? WinnerId);
