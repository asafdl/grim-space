using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Ids;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using System.Runtime.CompilerServices;

namespace GrimSpace.Tests;

internal static class BattleTestFixture
{
	public const int DefaultGridSize = 12;

	private static readonly ConditionalWeakTable<BattleOrchestrator, PresentationFrameBuilder> FrameBuilderCache = new();

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
		actorRuntimes.For(BattleActorIds.Rules);

		var engine = new Engine<BattleWorld, ActorRuntime>(world, actorRuntimes);
		var battle = new BattleOrchestrator(
			engine,
			layout,
			player.State.Id,
			EObjective.EliminateOpponents);
		foreach (var unit in units)
		{
			ExecutionAgent<BattleWorld, ActorRuntime>.Initialize(
				unit.ExecutionAgent,
				unit.State.Id,
				battle.Engine.CreateSimulation,
				battle.WriterFor(unit.State.Id));
		}

		battle.EnterPlayerTurn();
		return battle;
	}

	internal static void GrantPlayerPlanning(BattleOrchestrator battle) =>
		battle.GrantPlayerCanWork();

	internal static void RevokePlayerPlanning(BattleOrchestrator battle) =>
		battle.RevokePlayerCanWork();

	internal static void ResetPlayerPlanning(BattleOrchestrator battle)
	{
		RevokePlayerPlanning(battle);
		GrantPlayerPlanning(battle);
	}

	public static async Task<IReadOnlyList<IAction>> AwaitUnitActions(
		BattleOrchestrator battle,
		Unit unit)
	{
		var actorId = unit.State.Id;
		unit.ExecutionAgent.SetCanWork(true);
		try
		{
			var result = await battle.WaitForBatchAsync(actorId);
			if (!result.IsSuccess)
				throw result.Failure!;
			return result.Batch!.Actions;
		}
		finally
		{
			unit.ExecutionAgent.SetCanWork(false);
		}
	}

	public static string FirstEnemyId(BattleOrchestrator battle) =>
		UnitRegistry.For(battle.Engine.World)
			.All.First(unit => unit.Alliance.Team == ETeam.Enemy)
			.State.Id;

	public static PresentationFrameBuilder FrameBuilder(BattleOrchestrator battle) =>
		FrameBuilderCache.GetValue(battle, static _ => new PresentationFrameBuilder());

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

	public static Unit Carrier(Coord position, int momentum = 0) =>
		Create(Alliance.Enemy, "carrier", position, momentum, EType.Carrier);

	public static Unit Patrol(Coord position, int momentum = 0, string id = "patrol") =>
		Create(Alliance.Enemy, id, position, momentum, EType.Patrol);

	public static BattleOrchestrator BeginCarrierVsPlayer(
		Coord playerPos,
		Coord carrierPos,
		BoundedGrid? grid = null,
		IReadOnlySet<Coord>? blocked = null)
	{
		var player = Player(playerPos);
		var carrier = Carrier(carrierPos);
		return BeginSimulation(player, carrier, grid, blocked);
	}

	public static MovePathSession Path(string actorId, Coord origin, int pathApSpent, params Coord[] deltas)
	{
		var basis = BodyFrame.WorldAligned(origin);
		var gridBasis = GridBasis.From(basis.Fore, basis.Dorsal, basis.Starboard);
		var position = origin;
		var steps = new List<MoveStepAction>();
		var checkpoints = new List<MoveCheckpoint> { new(origin, gridBasis) };

		foreach (var delta in deltas)
		{
			EHeadingTurn? heading = null;
			if (delta != gridBasis.Forward)
			{
				heading = Enum.GetValues<EHeadingTurn>()
					.FirstOrDefault(turn => Orientation.HeadingTurn(gridBasis, turn).Forward == delta);
			}

			var step = new MoveStepAction(actorId, heading);
			steps.Add(step);
			gridBasis = heading is { } turn
				? Orientation.HeadingTurn(gridBasis, turn)
				: gridBasis;
			position += gridBasis.Forward;
			checkpoints.Add(new MoveCheckpoint(position, gridBasis));
		}

		var result = Player(origin).State.Clone();
		result.Position = position;
		result.Fore = gridBasis.Forward;
		result.Dorsal = gridBasis.Up;
		result.Starboard = gridBasis.Right;
		result.ActionPoints = System.Math.Max(0, result.ActionPoints - pathApSpent);
		return new MovePathSession(actorId, steps, checkpoints, result.ActionPoints, result);
	}

	public static MovePathSession ForwardPath(
		string actorId,
		Coord origin,
		int steps,
		int pathApSpent = 0) =>
		Path(actorId, origin, pathApSpent, Enumerable.Repeat(Coord.Forward, steps).ToArray());

	private static Unit Create(
		Alliance alliance,
		string id,
		Coord position,
		int momentum,
		EType type = EType.Fighter)
	{
		var instance = new Instance
		{
			Id = id,
			Type = type,
			Alliance = alliance,
		};

		return Factory.Create(
			instance,
			position,
			alliance.Team == ETeam.Player ? new UserExecutionAgent() : new AiController(),
			initialMomentum: momentum);
	}

	private static Unit WithAp(Unit unit, int actionPoints)
	{
		unit.State.ActionPoints = actionPoints;
		return unit;
	}
}
