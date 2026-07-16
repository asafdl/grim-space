using GrimSpace.Battle.Board;
using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle;

public sealed class Manager
{
	public BoundedGrid Grid { get; }
	public Turn.Manager Turn { get; }
	public IReadOnlyList<Unit> Units { get; }
	public PlayerController Player { get; }
	public HazardSystem Hazards { get; }
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }
	public bool IsResolving { get; private set; }

	private readonly Pipeline _pipeline;

	public Manager(
		BoundedGrid grid,
		Turn.Manager turn,
		IReadOnlyList<Unit> units,
		PlayerController player,
		HazardSystem hazards,
		Pipeline pipeline)
	{
		Grid = grid;
		Turn = turn;
		Units = units;
		Player = player;
		Hazards = hazards;
		_pipeline = pipeline;
	}

	public static Manager FromEncounter(Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var turn = new Turn.Manager();
		var hazards = new HazardSystem();
		var ids = new UnitIdRegistry();

		hazards.RegisterBoard(
			encounter.BoardHazards.Select(spawn =>
				Hazard.Asteroid(
					ids.NextNonUnitId("asteroid"),
					spawn.Center,
					grid,
					spawn.Radius,
					spawn.VisualId)));

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(spawn.Unit, spawn.Position, ids, spawn.InitialMomentum))
			.ToArray();

		var firstPlayer = units.FirstOrDefault(u => u.Controller == EController.Player);
		if (firstPlayer is not null)
			turn.SetActiveUnit(firstPlayer.State.Id);

		var blockedCells = hazards.GetBlockedCells();
		var pipeline = new Pipeline(grid, units, turn, hazards);

		var player = units.First(u => u.Controller == EController.Player);
		var enemy = units.First(u => u.Controller == EController.Enemy);

		Manager? self = null;
		var playerController = new PlayerController(
			player,
			enemy,
			units,
			hazards.NonUnits,
			grid,
			blockedCells,
			unit => self!.CanAct(unit),
			() => self!.GetActivePlayer());

		self = new Manager(grid, turn, units, playerController, hazards, pipeline);

		if (self.GetPlayer() is not null)
			self.Player.BeginTurn();

		return self;
	}

	public Unit? GetPlayer() =>
		Units.FirstOrDefault(u => u.Controller == EController.Player);

	public Unit? GetEnemy() =>
		Units.FirstOrDefault(u => u.Controller == EController.Enemy);

	private Unit? GetActivePlayer() =>
		GetActiveUnits().FirstOrDefault(u => u.Controller == EController.Player);

	public bool ExecuteTurn(FinalizedPlan playerPlan, IPresentationEventSink? sink = null)
	{
		if (IsBattleOver || IsResolving)
			return false;

		IsResolving = true;
		try
		{
			var result = _pipeline.Resolve(playerPlan, sink);
			IsBattleOver = result.IsBattleOver;
			WinnerId = result.WinnerId;

			if (GetPlayer() is not null)
				Player.BeginTurn();

			return true;
		}
		finally
		{
			IsResolving = false;
		}
	}

	private bool CanAct(Unit unit) =>
		!IsBattleOver && !IsResolving && Turn.IsActive(unit.State.Id) && unit.State.IsAlive;

	private IEnumerable<Unit> GetActiveUnits() =>
		Units.Where(u => Turn.IsActive(u.State.Id) && u.State.IsAlive);
}
