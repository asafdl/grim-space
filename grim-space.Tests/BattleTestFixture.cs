using GrimSpace.Battle;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class BattleTestFixture
{
	public const int DefaultGridSize = 12;

	public static BoundedGrid Grid(int size = DefaultGridSize) => new(size, size, size);

	public static BattleOrchestrator BeginPlanning(
		Unit player,
		Unit enemy,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null)
	{
		grid ??= Grid();
		blocked ??= new HashSet<Coord> { enemy.State.Position };

		var timeline = new Timeline();
		var hazards = new HazardSystem();
		var units = new Unit[] { player, enemy };
		var battle = new BattleOrchestrator(grid, timeline, units, player, enemy, hazards, blocked);
		battle.SetActiveUnit(player.State.Id);
		battle.BeginTurn(0);
		return battle;
	}

	public static BattleOrchestrator BeginPlanning(Coord origin, int momentum = 0)
	{
		var player = Player(origin, momentum: momentum);
		var enemy = Enemy(new Coord(0, 0, 0));
		return BeginPlanning(player, enemy);
	}

	public static Unit Player(
		Coord position,
		int momentum = 0,
		int actionPoints = 4) =>
		WithAp(Create(EController.Player, "player", position, momentum), actionPoints);

	public static Unit Enemy(Coord position, int momentum = 0) =>
		Create(EController.Enemy, "enemy", position, momentum);

	public static Option Path(Coord origin, int apCost, params Coord[] deltas)
	{
		var cells = new List<Coord>(deltas.Length);
		var pos = origin;

		foreach (var delta in deltas)
		{
			pos += delta;
			cells.Add(pos);
		}

		return new Option { Path = cells, ApCost = apCost };
	}

	public static Option ForwardPath(Coord origin, int steps, int apCost = 0) =>
		Path(origin, apCost, Enumerable.Repeat(Coord.Forward, steps).ToArray());

	private static Unit Create(EController controller, string id, Coord position, int momentum)
	{
		var instance = new Instance
		{
			Id = id,
			Type = EType.Fighter,
			Controller = controller,
		};

		return Factory.Create(instance, position, initialMomentum: momentum);
	}

	private static Unit WithAp(Unit unit, int actionPoints)
	{
		unit.State.ActionPoints = actionPoints;
		return unit;
	}
}
