using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ai;

public sealed class AiVsAiRunner
{
public static async Task<BattleResult> RunAsync(
		BattleEncounter encounter,
		int maxTurns = 50,
		Action<int, string>? onTurnLog = null)
	{
		var orchestrator = BattleOrchestrator.FromEncounter(encounter);
		var sw = Stopwatch.StartNew();

		var phaseTcs = new TaskCompletionSource<EBattlePhase>();
		orchestrator.PhaseChanged += phase =>
		{
			phaseTcs.TrySetResult(phase);
			phaseTcs = new TaskCompletionSource<EBattlePhase>();
		};

		for (var turn = 0; turn < maxTurns && !orchestrator.IsBattleOver; turn++)
		{
			if (orchestrator.Phase == EBattlePhase.PlayerTurn)
			{
				var activeUnit = UnitRegistry.For(orchestrator.Engine.World).UnitOf(orchestrator.ActivePlayerId);
				if (activeUnit.ExecutionAgent is AiExecutionAgent aiAgent)
				{
					await WaitForAgentAsync(aiAgent);
					aiAgent.Commit();
					orchestrator.EndTurn();
				}
			}

			// Let the orchestrator handle Resolving/Replaying phases naturally
			while (orchestrator.Phase == EBattlePhase.Resolving || orchestrator.Phase == EBattlePhase.Replaying)
			{
				await Task.WhenAny(phaseTcs.Task, Task.Delay(100));
				
				// In headless mode, immediately complete replay
				if (orchestrator.Phase == EBattlePhase.Replaying)
				{
					orchestrator.NotifyReplayComplete();
				}
				
				if (sw.ElapsedMilliseconds > 300000)
					throw new TimeoutException("Battle resolution timeout");
			}

			onTurnLog?.Invoke(turn, $"Turn {turn}: Phase={orchestrator.Phase}, Result={orchestrator.Engine.World.battleResult}");
		}

		sw.Stop();
		return new BattleResult
		{
			Winner = orchestrator.Engine.World.battleResult,
			Turns = orchestrator.TurnNumber,
			DurationMs = sw.ElapsedMilliseconds,
			PlayerUnits = CountAlive(orchestrator, ETeam.Player),
			EnemyUnits = CountAlive(orchestrator, ETeam.Enemy)
		};
	}

	private static async Task WaitForAgentAsync(AiExecutionAgent agent, int timeoutMs = 5000)
	{
		var sw = Stopwatch.StartNew();
		while (agent.IsPlanning && sw.ElapsedMilliseconds < timeoutMs)
		{
			await Task.Delay(16);
		}
		if (agent.IsPlanning)
			throw new TimeoutException("AI agent planning timeout");
	}

	private static int CountAlive(BattleOrchestrator orchestrator, ETeam team)
	{
		var units = UnitRegistry.For(orchestrator.Engine.World);
		return units.All.Count(u => u.Team == team && u.State.IsAlive);
	}

	public static BattleEncounter CreateAiVsAiEncounter(int seed = 42, int gridSize = 64)
	{
		var (playerSpawn, enemySpawn) = DeploymentPlacement.DevDuel(
			EType.Fighter,
			EType.Carrier,
			seed,
			gridSize,
			$"fighter-ai-{seed}",
			$"carrier-ai-{seed}");

		// Override positions to be within weapon range (LightningCannon effective=9)
		// Place at ~6 cells apart (60-70% of effective range)
		var center = new Coord(gridSize / 2, gridSize / 2, gridSize / 2);
		var playerPos = center - new Coord(3, 0, 0);
		var enemyPos = center + new Coord(3, 0, 0);

		playerSpawn = new BattleSpawn
		{
			Ship = playerSpawn.Ship,
			Pilot = playerSpawn.Pilot,
			Team = playerSpawn.Team,
			Position = playerPos,
			Fore = new Coord(1, 0, 0),
			Dorsal = Coord.Up,
			ExecutionAgent = new AiExecutionAgent()
		};

		enemySpawn = new BattleSpawn
		{
			Ship = enemySpawn.Ship,
			Pilot = enemySpawn.Pilot,
			Team = enemySpawn.Team,
			Position = enemyPos,
			Fore = new Coord(-1, 0, 0),
			Dorsal = Coord.Up,
			ExecutionAgent = new AiController()
		};

		var gunshipSpawn = new BattleSpawn
		{
			Ship = DeploymentPlacement.UpgradeDevShip(
				ShipInstance.FromCatalog($"gunship-ai-{seed}", EType.Gunship)),
			Team = ETeam.Player,
			Position = playerPos + new Coord(0, 0, 2),
			Fore = new Coord(1, 0, 0),
			Dorsal = Coord.Up,
			ExecutionAgent = new AiExecutionAgent(),
		};

		var gooperSpawn = new BattleSpawn
		{
			Ship = DeploymentPlacement.UpgradeDevShip(
				ShipInstance.FromCatalog($"industrial-gooper-ai-{seed}", EType.IndustrialGooper)),
			Team = ETeam.Enemy,
			Position = enemyPos + new Coord(0, 0, 2),
			Fore = new Coord(-1, 0, 0),
			Dorsal = Coord.Up,
			ExecutionAgent = new AiController(),
		};

		var spawns = new[] { playerSpawn, gunshipSpawn, enemySpawn, gooperSpawn };
		var fieldMargin = 2;
		var fieldCenter = new Coord(gridSize / 2, gridSize / 2, gridSize / 2);

		return new BattleEncounter
		{
			Id = TypedIdGenerator.NextId("engagement-ai-vs-ai"),
			Seed = seed,
			Spawns = spawns,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = seed,
				GridSize = gridSize,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				RegionCenter = fieldCenter,
				RegionHalfExtent = gridSize / 2 - fieldMargin,
				RegionMargin = fieldMargin,
				ReservedCells = AsteroidFieldReservations.PlayerCorridors(spawns),
			}),
		};
	}

	private static Coord FlankPosition(BattleSpawn spawn, int gridSize)
	{
		var lateral = Coord.Cross(spawn.Dorsal, spawn.Fore);
		if (lateral == Coord.Zero)
			lateral = new Coord(1, 0, 0);

		var distance = System.Math.Min(4, gridSize - 1);
		var positive = spawn.Position + lateral * distance;
		if (IsInBounds(positive, gridSize))
			return positive;

		var negative = spawn.Position - lateral * distance;
		return IsInBounds(negative, gridSize) ? negative : spawn.Position;
	}

	private static bool IsInBounds(Coord position, int gridSize) =>
		position.X >= 0 && position.X < gridSize
		&& position.Y >= 0 && position.Y < gridSize
		&& position.Z >= 0 && position.Z < gridSize;
}

public sealed class BattleResult
{
	public EBattleResult Winner { get; init; }
	public int Turns { get; init; }
	public long DurationMs { get; init; }
	public int PlayerUnits { get; init; }
	public int EnemyUnits { get; init; }

	public override string ToString() =>
		$"BattleResult: Winner={Winner}, Turns={Turns}, Duration={DurationMs}ms, PlayerAlive={PlayerUnits}, EnemyAlive={EnemyUnits}";
}