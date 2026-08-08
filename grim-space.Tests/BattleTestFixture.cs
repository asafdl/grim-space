using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal static class BattleTestFixture
{
	public const int DefaultGridSize = 12;

	public static BoundedGrid Grid(int size = DefaultGridSize) => new(size, size, size);

	public static BattleOrchestrator BeginSimulation(
		Unit player,
		Unit enemy,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null)
	{
		grid ??= Grid();
		blocked ??= new HashSet<Coord> { enemy.State.Position };

		var timeline = new Timeline();
		var nonUnits = new Dictionary<string, NonUnit>();
		var units = new Unit[] { player, enemy };
		var world = BattleWorld.FromLive(units, nonUnits, grid, blocked, timeline);
		var layout = BattleLayout.FromEncounter(grid, [], units);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(player.State.Id);
		actorRuntimes.For(enemy.State.Id);
		actorRuntimes.For(EntityIds.System);

		var engine = new Engine<BattleWorld, ActorRuntime>(world, actorRuntimes);
		var playerAgent = (HumanExecutionAgent)player.ExecutionAgent;
		var battle = new BattleOrchestrator(
			engine,
			layout,
			player.State.Id,
			enemy.State.Id,
			playerAgent,
			EObjective.EliminateOpponents);
		battle.SetActiveUnit(player.State.Id);
		battle.BeginTurn();
		return battle;
	}

	public static BattleUi Ui(BattleOrchestrator battle)
	{
		var ui = new BattleUi(battle);
		_ = ui.MoveUi;
		return ui;
	}

	public static BattleSimulation CreateTrialSimulation(BattleOrchestrator battle) =>
		battle.Engine.CreateSimulation();

	public static BattleOrchestrator BeginSimulation(Coord origin, int momentum = 0)
	{
		var player = Player(origin, momentum: momentum);
		var enemy = Enemy(origin + Coord.Forward * 6);
		return BeginSimulation(player, enemy);
	}

	public static Unit Player(
		Coord position,
		int momentum = 0,
		int actionPoints = 4) =>
		WithAp(Create(Alliance.Player, "player", position, momentum), actionPoints);

	public static Unit Enemy(Coord position, int momentum = 0) =>
		Create(Alliance.Enemy, "enemy", position, momentum);

	public static MovePathSession Path(string actorId, Coord origin, int pathApSpent, params Coord[] deltas)
	{
		var frame = BodyFrame.WorldAligned(origin);
		var session = MovePathSession.Begin(actorId, origin, frame, 0, Stats.ForType(EType.Fighter).MinPathApCost);
		var pos = origin;

		foreach (var delta in deltas)
		{
			var to = pos + delta;
			var direction = frame.DirectionOfStep(pos, to)
				?? throw new InvalidOperationException("Move step direction is undefined.");
			session.Steps.Add(new MoveStepAction(actorId, direction));
			session.Cells.Add(to);
			pos = to;
		}

		session.PathApSpent = pathApSpent;
		session.MinPathApRemaining = 0;
		return session;
	}

	public static MovePathSession ForwardPath(
		string actorId,
		Coord origin,
		int steps,
		int pathApSpent = 0) =>
		Path(actorId, origin, pathApSpent, Enumerable.Repeat(Coord.Forward, steps).ToArray());

	private static Unit Create(Alliance alliance, string id, Coord position, int momentum)
	{
		var instance = new Instance
		{
			Id = id,
			Type = EType.Fighter,
			Alliance = alliance,
		};

		return Factory.Create(instance, position, initialMomentum: momentum);
	}

	private static Unit WithAp(Unit unit, int actionPoints)
	{
		unit.State.ActionPoints = actionPoints;
		return unit;
	}
}
