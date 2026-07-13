using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Player;
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
	public HazardSystem Hazards { get; }
	public PlayerController Player { get; }
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }

	private readonly Pipeline _pipeline;

	private Manager(
		BoundedGrid grid,
		Turn.Manager turn,
		IReadOnlyList<Unit> units,
		HazardSystem hazards,
		PlayerController player,
		Pipeline pipeline)
	{
		Grid = grid;
		Turn = turn;
		Units = units;
		Hazards = hazards;
		Player = player;
		_pipeline = pipeline;
	}

	public static Manager FromEncounter(Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var turn = new Turn.Manager();
		var hazards = new HazardSystem();

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(spawn.Unit, spawn.Position, spawn.InitialMomentum))
			.ToArray();

		var firstPlayer = units.FirstOrDefault(u => u.Controller == EController.Player);
		if (firstPlayer is not null)
			turn.SetActiveUnit(firstPlayer.State.Id);

		var pipeline = new Pipeline(grid, units, turn, hazards);

		Manager? self = null;
		var playerController = new PlayerController(
			units.First(u => u.Controller == EController.Player),
			units.First(u => u.Controller == EController.Enemy),
			grid,
			unit => self!.CanAct(unit),
			() => self!.GetActivePlayer());

		self = new Manager(grid, turn, units, hazards, playerController, pipeline);

		if (self.GetPlayer() is { } player)
			self.Player.ResetFrom(player.State);

		return self;
	}

	public Unit? GetPlayer() =>
		Units.FirstOrDefault(u => u.Controller == EController.Player);

	public Unit? GetEnemy() =>
		Units.FirstOrDefault(u => u.Controller == EController.Enemy);

	public Unit? GetActivePlayer() =>
		GetActiveUnits().FirstOrDefault(u => u.Controller == EController.Player);

	public bool ExecuteTurn(FinalizedPlan playerPlan)
	{
		if (IsBattleOver)
			return false;

		var result = _pipeline.Resolve(playerPlan, GetPlayer, GetEnemy);
		IsBattleOver = result.IsBattleOver;
		WinnerId = result.WinnerId;

		if (GetPlayer() is { } player)
			Player.ResetFrom(player.State);

		return true;
	}

	public bool CanAct(Unit unit) =>
		!IsBattleOver && Turn.IsActive(unit.State.Id) && unit.State.IsAlive;

	public IEnumerable<Unit> GetActiveUnits() =>
		Units.Where(u => Turn.IsActive(u.State.Id) && u.State.IsAlive);
}
