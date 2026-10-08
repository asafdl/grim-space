using System.Collections.Frozen;
using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class GoopGunActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ResolveSetsCooldownToUnavailableTurnsPlusOne()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			BattleSpawnTestKit.FighterWithGoopGun(PlayerId),
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));
		var spec = CatalogExpectations.DefaultGoopGunSpec();
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(
			spec.UnavailableTurns + 1,
			StateMountTestKit.CooldownRemaining(
				battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId),
				EAbilityKind.GoopGun,
				ESpatialOrientation.Forward));
	}

	[Fact]
	public void BindAllocatesTypedGoopHazardId()
	{
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);

		Assert.StartsWith($"{NonUnitTypeSlug.Goop}-", action.GoopHazardId);
	}

	[Fact]
	public void ExecutionRebindsPreviewGoopHazardId()
	{
		var preview = new GoopGunAction(
			PlayerId,
			ESpatialOrientation.Forward,
			Capabilities.PreviewGoopHazardId);
		var choice = new AbilityActivationChoice(
			preview,
			Coord.Zero,
			Coord.Forward,
			Coord.Up,
			AbilityHudCatalog.ForUnit(EType.Fighter)[0].Targeting);

		var execution = Assert.IsType<GoopGunAction>(AbilityActivation.CreateExecutionAction(choice));

		Assert.NotEqual(Capabilities.PreviewGoopHazardId, execution.GoopHazardId);
		Assert.StartsWith($"{NonUnitTypeSlug.Goop}-", execution.GoopHazardId);
	}

	[Fact]
	public void LegalCapabilitiesUsePreviewGoopHazardId()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			BattleSpawnTestKit.FighterWithGoopGun(PlayerId),
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));

		var goop = Assert.IsType<GoopGunAction>(
			Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId)
				.Single(action => action is GoopGunAction));

		Assert.Equal(Capabilities.PreviewGoopHazardId, goop.GoopHazardId);
	}

	[Fact]
	public void ResolveSpawnsGoopHazardWithAffectedCells()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = CreateGoopGunOrchestrator(playerPos, playerPos + Coord.Forward * 10);
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);
		var expectedCells = GoopGunDef.Instance.AffectedCells(action, battle.PlayerAgent.Sim.World);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(keepRecords: true, action));
		Assert.True(battle.PlayerAgent.Sim.World.NonUnits.TryGetValue(action.GoopHazardId, out var nonUnit));
		var hazard = Assert.IsType<GoopHazard>(nonUnit);
		Assert.Equal(PlayerId, hazard.ActorId);
		Assert.Equal(expectedCells, hazard.Cells);
		Assert.Equal(hazard.Center, hazard.Frame.Origin);
		var burstAxis = BodyFrame.From(battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId))
			.Step(ESpatialOrientation.Forward);
		Assert.Equal(burstAxis, hazard.Frame.Fore);
		Assert.All(
			hazard.Cells,
			cell => Assert.Equal(0, Coord.Dot(cell - hazard.Center, burstAxis)));
		Assert.Contains(
			new Record<GoopSpawnedFacts>(new GoopSpawnedFacts(
				PlayerId,
				action.GoopHazardId,
				hazard.Center,
				hazard.Cells)),
			battle.PlayerAgent.Sim.RecordsFor(0));
	}

	[Fact]
	public void GoopHazardSurvivesFollowingTurnThenDissipates()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = CreateGoopGunOrchestrator(playerPos, playerPos + Coord.Forward * 10);
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));
		BattleTestActions.CommitAndResolve(battle);

		Assert.Contains(action.GoopHazardId, battle.Engine.World.NonUnits.Keys);

		battle.Engine.AdvanceTick();

		Assert.DoesNotContain(action.GoopHazardId, battle.Engine.World.NonUnits.Keys);
	}

	[Fact]
	public void AffectedCellsExcludeCellsBehindGoopOnBurstAxis()
	{
		var playerPos = new Coord(5, 5, 5);
		var pathGoop = playerPos + Coord.Forward * 2;
		var behindPathGoop = playerPos + Coord.Forward * 4;
		var battle = CreateGoopGunOrchestrator(playerPos, playerPos + Coord.Forward * 10);
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(world, CreateGoop("path-goop", pathGoop));
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);
		var affected = GoopGunDef.Instance.AffectedCells(action, world);

		Assert.DoesNotContain(behindPathGoop, affected);
	}

	[Fact]
	public void AffectedCellsExcludeAsteroidOnBurstAndShadowedCells()
	{
		var playerPos = new Coord(5, 5, 5);
		var asteroidPos = playerPos + Coord.Forward * 3;
		var behind = playerPos + Coord.Forward * 6;
		var battle = CreateGoopGunOrchestrator(playerPos, behind);
		var grid = battle.Engine.World.Grid;
		var world = battle.PlayerAgent.Sim.World;
		BattleTestWorld.InjectNonUnit(
			world,
			Asteroid.Create("asteroid", asteroidPos, grid, [asteroidPos]));
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);
		var affected = GoopGunDef.Instance.AffectedCells(action, world);

		Assert.DoesNotContain(asteroidPos, affected);
		Assert.DoesNotContain(behind, affected);
		Assert.Contains(asteroidPos + Coord.Up, affected);
	}

	private static GoopHazard CreateGoop(string id, Coord cell) =>
		new()
		{
			Id = id,
			ActorId = PlayerId,
			Center = cell,
			Frame = BodyFrame.WorldAligned(cell),
			Cells = new HashSet<Coord> { cell }.ToFrozenSet(),
		};

	private static BattleOrchestrator CreateGoopGunOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new BattleEncounter
		{
			Id = "goop-gun-test",
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				BattleSpawnTestKit.Create(
					BattleSpawnTestKit.FighterWithGoopGun(PlayerId),
					ETeam.Player,
					playerPos,
					new UserExecutionAgent()),
				BattleSpawnTestKit.Create(
					"enemy",
					EType.Fighter,
					ETeam.Enemy,
					enemyPos,
					new AiController()),
			],
		};

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 30);
	}
}
