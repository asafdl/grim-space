using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class PresentationFrameTests
{
	[Fact]
	public void FrameAfterQueuedMoveShowsReachableExtensions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var options = BattleTestCommands.MoveOptions(battle).ToList();
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));

		var frame = BattleTestCommands.Frame(battle);
		var endpoints = frame.MovePaths.Select(option => option.EndPosition).ToHashSet();

		Assert.Equal(threeStepEnd, frame.FocusState.Position);
		Assert.Contains(origin + Coord.Forward * 4, endpoints);
		Assert.Equal(threeStepEnd, frame.MoveTarget);
		Assert.Equal(3, frame.MoveCheckpoints.Count);
	}

	[Fact]
	public void UndoClearsQueuedMoveFromFrame()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		BattleTestCommands.Move(battle, threeStepEnd);
		Assert.True(BattleTestCommands.Undo(battle));

		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(origin, frame.FocusState.Position);
		Assert.Null(frame.MoveTarget);
		Assert.Empty(frame.MoveCheckpoints);
		Assert.Contains(
			frame.MovePaths,
			option => option.EndPosition == origin + Coord.Forward * 4);
	}

	[Fact]
	public void UndoClearsQueuedLightningCannon()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));

		Assert.True(BattleTestCommands.FireLightningCannon(battle));
		Assert.Equal(0, StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId), GrimSpace.Units.Loadouts.Abilities.EAbilityKind.LightningCannon));
		Assert.Single(battle.PlayerAgent.Sim.Actions);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.LightningCannon), StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId), EAbilityKind.LightningCannon));
	}

	[Fact]
	public void UndoClearsQueuedScrapDroneSwarm()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginScrapSimulation(origin);

		Assert.True(BattleTestCommands.FireScrapDroneSwarm(battle, ESpatialOrientation.Port));
		Assert.Equal(
			0,
			StateMountTestKit.UsesRemaining(
				battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId),
				EAbilityKind.ScrapDroneSwarm,
				ESpatialOrientation.Port));

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.RepurposedMiner, EAbilityKind.ScrapDroneSwarm), StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId), EAbilityKind.ScrapDroneSwarm));
	}

	[Fact]
	public void UndoClearsLightningCannonQueuedAfterMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		Assert.True(BattleTestCommands.FireLightningCannon(battle));

		Assert.Equal(4, battle.PlayerAgent.Sim.Actions.Count);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.DoesNotContain(battle.PlayerAgent.Sim.Actions, action => action is LightningCannonAction);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.LightningCannon), StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId), EAbilityKind.LightningCannon));
		Assert.Equal(3, battle.PlayerAgent.Sim.Actions.Count(action => action is MoveStepAction));
	}

	[Fact]
	public void UndoLightningCannonPreservesCommittedMoveCheckpoints()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		var preview = new PlanningPreview();
		var pathAfterMove = preview.CommittedMoveCheckpoints(battle.PlayerAgent.Sim, battle.PlayerId).ToList();
		Assert.NotEmpty(pathAfterMove);

		Assert.True(BattleTestCommands.FireLightningCannon(battle));
		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Equal(pathAfterMove, preview.CommittedMoveCheckpoints(battle.PlayerAgent.Sim, battle.PlayerId));
		Assert.DoesNotContain(battle.PlayerAgent.Sim.Actions, action => action is LightningCannonAction);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.LightningCannon), StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId), EAbilityKind.LightningCannon));
	}

	[Fact]
	public void DefaultFocusIsPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));

		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.True(frame.CanAct);
		Assert.True(frame.ShowMovePreview);
		Assert.Equal(battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position, frame.FocusState.Position);
		Assert.NotEmpty(frame.MovePaths);
	}

	[Fact]
	public void FocusEnemyShowsInspectionFrame()
	{
		var origin = new Coord(5, 5, 5);
		var enemyPos = TurnOrchestrationTests.EnemyInLightningCannonLine(origin);
		var battle = CreateOrchestrator(origin, enemyPos);

		BattleTestCommands.Focus(battle, BattleTestFixture.FirstEnemyId(battle));
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(BattleTestFixture.FirstEnemyId(battle), frame.FocusId);
		Assert.True(frame.IsInspecting);
		Assert.False(frame.CanAct);
		Assert.Equal(EPlayerMode.Move, frame.Mode);
		Assert.True(frame.ShowMovePreview);
		Assert.Empty(frame.MoveCheckpoints);
		Assert.Null(frame.MoveTarget);
		Assert.False(frame.ShowWeaponPreviews);
		Assert.Equal(enemyPos, frame.FocusState.Position);
		Assert.NotEmpty(frame.MovePaths);
	}

	[Fact]
	public void InspectionDoesNotMutatePlanning()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;
		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));

		var actionsBefore = battle.PlayerAgent.Sim.Actions.ToList();
		var playerPosBefore = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position;

		BattleTestCommands.Focus(battle, BattleTestFixture.FirstEnemyId(battle));
		_ = BattleTestCommands.Frame(battle);

		Assert.Equal(actionsBefore, battle.PlayerAgent.Sim.Actions);
		Assert.Equal(playerPosBefore, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void QueuedAreaActionsUseTheirOwnActorStateAtQueueIndex()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginScrapSimulation(origin);
		var afterMove = origin + Coord.Forward * 2;

		Assert.True(BattleTestCommands.FireScrapDroneSwarm(battle, ESpatialOrientation.Port));
		Assert.True(BattleTestCommands.Move(battle, afterMove));
		Assert.True(BattleTestCommands.FireScrapDroneSwarm(battle, ESpatialOrientation.Starboard));

		var preview = new PlanningPreview();
		Assert.Equal(
			afterMove,
			preview.PreviewUnits(battle.PlayerAgent.Sim, battle.PlayerId)[battle.PlayerId].Position);

		var frame = BattleTestCommands.Frame(battle);
		var queued = frame.AreaActions.Queued
			.Where(entry => entry.Action is ScrapDroneSwarmAction)
			.ToList();
		Assert.Equal(2, queued.Count);
		foreach (var swarmPreview in queued)
		{
			var swarmIndex = battle.PlayerAgent.Sim.Actions
				.ToList()
				.FindIndex(action => action == swarmPreview.Action);
			var expectedSwarmBurst = ScrapDroneSwarmDef.Instance.AffectedCells(
				(ScrapDroneSwarmAction)swarmPreview.Action,
				battle.PlayerAgent.Sim.ReplayWorld(swarmIndex));
			Assert.True(expectedSwarmBurst.SetEquals(swarmPreview.Volume.Cells));
		}

		Assert.Equal(origin, queued[0].Volume.Origin);
		Assert.Equal(afterMove, queued[1].Volume.Origin);
	}

	[Fact]
	public void FramePublishesAuthoritativeBoardClippedAreaActions()
	{
		var origin = new Coord(10, 10, 10);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.ScrapPlayer(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));

		var frame = BattleTestCommands.Frame(battle);
		var swarmPreviews = frame.AreaActions.Aim
			.Where(preview => preview.Action is ScrapDroneSwarmAction)
			.ToDictionary(
				preview => ((ScrapDroneSwarmAction)preview.Action).MountedOn,
				preview => preview.Volume);
		Assert.Equal(
			frame.Weapons.PortScrapDroneSwarm,
			swarmPreviews.ContainsKey(ESpatialOrientation.Port));
		Assert.Equal(
			frame.Weapons.StarboardScrapDroneSwarm,
			swarmPreviews.ContainsKey(ESpatialOrientation.Starboard));
		foreach (var (mountedOn, volume) in swarmPreviews)
		{
			var expectedSwarmBurst = ScrapDroneSwarmDef.Instance.AffectedCells(
				new ScrapDroneSwarmAction(battle.PlayerId, mountedOn),
				battle.PlayerAgent.Sim.World);
			Assert.True(expectedSwarmBurst.SetEquals(volume.Cells));
			Assert.Equal(origin, volume.Origin);
			Assert.All(
				volume.Cells,
				cell => Assert.True(battle.PlayerAgent.Sim.World.Grid.IsInBounds(cell)));
		}
	}

	[Fact]
	public void AreaActionDefinitionsUseActorPoseAndBoundMount()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginScrapSimulation(origin);
		var world = battle.PlayerAgent.Sim.World;
		var actor = world.StateOf(battle.PlayerId);
		actor.Fore = new Coord(1, 0, 0);
		actor.Dorsal = Coord.Up;
		actor.Starboard = Coord.Cross(actor.Dorsal, actor.Fore);
		var frame = BodyFrame.From(actor);
		var port = new ScrapDroneSwarmAction(battle.PlayerId, ESpatialOrientation.Port);
		var starboard = new ScrapDroneSwarmAction(battle.PlayerId, ESpatialOrientation.Starboard);

		var portCells = ((IAreaActionDef)port.Definition).AffectedCells(port, world);
		var starboardCells = ((IAreaActionDef)starboard.Definition).AffectedCells(starboard, world);

		Assert.Contains(frame.ToWorld(0, 1, 0), portCells);
		Assert.DoesNotContain(frame.ToWorld(0, -1, 0), portCells);
		Assert.Contains(frame.ToWorld(0, -1, 0), starboardCells);
		Assert.DoesNotContain(frame.Origin, starboardCells);
	}

	[Fact]
	public void QueuedTorpedoShowsSpawnedUnitWithoutTravelPreview()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			VoidBombDef.Instance.Bind(battle.PlayerId, ESpatialOrientation.Ventral)));

		var action = Assert.IsType<VoidBombAction>(Assert.Single(battle.PlayerAgent.Sim.Actions));
		Assert.NotNull(action.SpawnedUnitId);
		var frame = BattleTestCommands.Frame(battle);

		Assert.Null(frame.VoidBombPreviews.Queued);
		Assert.Contains(
			frame.PreviewUnits.Values,
			unit => unit.Type == EType.VoidBomb);
	}

	[Fact]
	public void HoveredTorpedoPublishesDisjointTurnVolumes()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var frames = BattleTestFixture.FrameBuilder(battle);
		var spec = AbilityHudCatalog.ForUnit(
				battle.PlayerAgent.Sim.World.StateOf(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.VoidBomb);
		frames.Interaction.SetMode(EPlayerMode.VoidBomb, spec);
		var choices = BattleTestCommands.Frame(battle).AbilityChoices;
		var dorsalIndex = choices
			.Select((choice, index) => (choice, index))
			.Single(entry => entry.choice.Orientation == ESpatialOrientation.Ventral)
			.index;
		frames.Interaction.SetAbilityHover(dorsalIndex, choices.Count);

		var frame = BattleTestCommands.Frame(battle);
		var aim = frame.VoidBombPreviews.Aim;

		Assert.NotNull(aim);
		Assert.Null(frame.VoidBombPreviews.Queued);
		Assert.Equal(CatalogExpectations.DefaultVoidBombLauncher().FuelTurns, aim.TurnBands.Count);
		Assert.Equal(
			VoidBombMount.LaunchPose(
				battle.PlayerAgent.Sim.World.StateOf(battle.PlayerId),
				ESpatialOrientation.Ventral).Position,
			aim.Origin);
		for (var i = 0; i < aim.TurnBands.Count; i++)
		{
			for (var j = i + 1; j < aim.TurnBands.Count; j++)
				Assert.Empty(aim.TurnBands[i].Intersect(aim.TurnBands[j]));
		}
	}

	[Fact]
	public void TorpedoFirstTurnBandContainsSameCycleEndpoint()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var frames = BattleTestFixture.FrameBuilder(battle);
		var spec = AbilityHudCatalog.ForUnit(
				battle.PlayerAgent.Sim.World.StateOf(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.VoidBomb);
		frames.Interaction.SetMode(EPlayerMode.VoidBomb, spec);
		var choices = BattleTestCommands.Frame(battle).AbilityChoices;
		var retroIndex = choices
			.Select((choice, index) => (choice, index))
			.Single(entry => entry.choice.Orientation == ESpatialOrientation.Ventral)
			.index;
		frames.Interaction.SetAbilityHover(retroIndex, choices.Count);
		var aim = BattleTestCommands.Frame(battle).VoidBombPreviews.Aim;
		Assert.NotNull(aim);
		var firstLayer = aim.TurnBands[0];

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			VoidBombDef.Instance.Bind(battle.PlayerId, ESpatialOrientation.Ventral)));
		var launchedId = Assert.IsType<VoidBombAction>(Assert.Single(battle.PlayerAgent.Sim.Actions)).SpawnedUnitId;

		var replay = BattleTestActions.CommitAndResolve(battle);
		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Id == launchedId);

		Assert.Contains(
			replay.Actions,
			action => action is VoidBombMoveStepAction { ActorId: var id } && id == torpedo.State.Id);
		Assert.Contains(torpedo.State.Position, firstLayer);
	}

	[Fact]
	public void TurnVolumeBandsContainOnlyEarliestReachTurn()
	{
		var a = new Coord(1, 0, 0);
		var b = new Coord(2, 0, 0);
		var c = new Coord(3, 0, 0);
		var d = new Coord(4, 0, 0);
		IReadOnlyList<IReadOnlySet<Coord>> layers =
		[
			new HashSet<Coord> { a, b },
			new HashSet<Coord> { b, c },
			new HashSet<Coord> { a, c, d },
		];

		var preview = TurnVolumePreview.FromCumulativeReach(Coord.Zero, layers);

		Assert.True(preview.TurnBands[0].SetEquals([a, b]));
		Assert.True(preview.TurnBands[1].SetEquals([c]));
		Assert.True(preview.TurnBands[2].SetEquals([d]));
		Assert.True(
			layers
				.SelectMany(layer => layer)
				.ToHashSet()
				.SetEquals(preview.TurnBands.SelectMany(band => band)));
	}

	[Fact]
	public void VoidBombMountsExcludeBlockedLaunchCells()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var (blockedMount, _, _) = VoidBombMount.LaunchPose(
			player.State,
			ESpatialOrientation.Ventral);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position, blockedMount });

		var mounts = BattleTestCommands.Frame(battle).Weapons.VoidBombMounts;

		Assert.DoesNotContain(ESpatialOrientation.Ventral, mounts);
		Assert.Empty(mounts);
	}

	[Fact]
	public void ThreatenedUnitIdsIncludesTargetsFromEveryQueuedWeapon()
	{
		var origin = new Coord(5, 5, 5);
		var probe = BattleTestFixture.BeginScrapSimulation(origin);
		var burstCell = ScrapDroneSwarmDef.Instance.AffectedCells(
			new ScrapDroneSwarmAction(probe.PlayerId, ESpatialOrientation.Starboard),
			probe.PlayerAgent.Sim.World).First();
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.ScrapPlayer(origin),
			BattleTestFixture.Enemy(burstCell),
			BattleTestFixture.Grid(20));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var preview = new PlanningPreview();

		Assert.True(BattleTestCommands.FireScrapDroneSwarm(battle, ESpatialOrientation.Port));
		Assert.True(BattleTestCommands.FireScrapDroneSwarm(battle, ESpatialOrientation.Starboard));

		Assert.Contains(
			enemyId,
			preview.ThreatenedUnitIds(battle.PlayerAgent.Sim, battle.PlayerId));
	}

	[Fact]
	public void PreviewRetainsPredictedDeadUnits()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var enemy = battle.PlayerAgent.Sim.StateOf<ActorState>(enemyId);
		enemy.CurrentHullPoints = 1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			enemy.CurrentShieldPoints[face] = 0;

		Assert.True(BattleTestCommands.FireLightningCannon(battle));

		var preview = new PlanningPreview().PreviewUnits(battle.PlayerAgent.Sim, battle.PlayerId);

		Assert.True(preview.ContainsKey(enemyId));
		Assert.False(preview[enemyId].IsAlive);
	}

	[Fact]
	public void InvalidFocusTargetFallsBackToPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));

		BattleTestCommands.Focus(battle, "missing");
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.Null(BattleTestFixture.FrameBuilder(battle).Interaction.FocusId);
	}

	[Fact]
	public void MovementAssistMatchesReticleAndKeepsReferenceStableAcrossHover()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(BattleTestFixture.Player(origin),
			[
				BattleTestFixture.Enemy(origin + Coord.Forward * 4),
				BattleTestFixture.RepurposedMiner(origin - Coord.Forward * 5, "other-enemy"),
			]);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var initial = BattleTestCommands.Frame(battle);
		var destination = origin - Coord.Forward;
		Assert.NotNull(initial.MovementPickPreference);
		builder.Interaction.SetMoveHover(destination, initial.MovePaths);
		var hovered = BattleTestCommands.Frame(battle);
		Assert.Equal(initial.RangeEnemy!.Value.EnemyId, hovered.RangeEnemy!.Value.EnemyId);
		Assert.Same(initial.MovementPickPreference, hovered.MovementPickPreference);
		builder.Interaction.SetRangeEnemy("other-enemy");
		var retargeted = BattleTestCommands.Frame(battle);
		Assert.Equal("other-enemy", retargeted.RangeEnemy!.Value.EnemyId);
		Assert.NotSame(hovered.MovementPickPreference, retargeted.MovementPickPreference);
		var preference = retargeted.MovementPickPreference!(destination);
		Assert.Equal(MovementPick.Alignment(destination, origin - Coord.Forward * 5, preference.CanHit),
			preference);
		BattleTestCommands.Focus(battle, "other-enemy");
		Assert.Null(BattleTestCommands.Frame(battle).MovementPickPreference);
	}

	[Fact]
	public void RangeTargetUsesEuclideanMeterSeparation()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(1, 1, 1)),
			BattleTestFixture.Enemy(new Coord(4, 5, 7)));

		var target = BattleTestCommands.Frame(battle).RangeEnemy;

		Assert.True(target.HasValue);
		Assert.Equal(
			MathF.Sqrt(61f) * BattleDistance.MetersPerCell,
			target.Value.SeparationMeters,
			3);
	}

	[Fact]
	public void RangeTargetOriginFollowsMoveGhost()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var options = BattleTestCommands.Frame(battle).MovePaths;
		var hovered = options.First(option => option.EndPosition == origin + Coord.Forward);
		var enemyPosition = battle.PlayerAgent.Sim
			.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle))
			.Position;

		builder.Interaction.SetMoveHover(hovered.EndPosition, options);
		var target = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true).RangeEnemy;

		Assert.True(target.HasValue);
		Assert.Equal(
			BattleDistance.SeparationMeters(hovered.EndPosition, enemyPosition),
			target.Value.SeparationMeters,
			3);
	}

	[Fact]
	public void RequestedRangeTargetOverridesClosestEnemy()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(1, 1, 1)),
			[
				BattleTestFixture.Enemy(new Coord(2, 1, 1)),
				BattleTestFixture.RepurposedMiner(new Coord(8, 8, 8), "far-enemy"),
			]);
		BattleTestFixture.FrameBuilder(battle).Interaction.SetRangeEnemy("far-enemy");

		var target = BattleTestCommands.Frame(battle).RangeEnemy;

		Assert.True(target.HasValue);
		Assert.Equal("far-enemy", target.Value.EnemyId);
	}

	[Fact]
	public void DeadRequestedRangeTargetFallsBackToClosestLivingEnemy()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(1, 1, 1)),
			[
				BattleTestFixture.Enemy(new Coord(2, 1, 1)),
				BattleTestFixture.RepurposedMiner(new Coord(8, 8, 8), "far-enemy"),
			]);
		BattleTestFixture.FrameBuilder(battle).Interaction.SetRangeEnemy("far-enemy");
		battle.Engine.World.StateOf("far-enemy").CurrentHullPoints = 0;
		battle.PlayerAgent.Sim.StateOf<ActorState>("far-enemy").CurrentHullPoints = 0;

		var target = BattleTestCommands.Frame(battle).RangeEnemy;

		Assert.True(target.HasValue);
		Assert.Equal("enemy", target.Value.EnemyId);
		Assert.Null(BattleTestFixture.FrameBuilder(battle).Interaction.RangeEnemyId);
	}

	[Fact]
	public void PredictedDeadRequestedRangeTargetStaysSelectable()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var simEnemy = battle.PlayerAgent.Sim.StateOf<ActorState>(enemyId);
		simEnemy.CurrentHullPoints = 1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			simEnemy.CurrentShieldPoints[face] = 0;

		BattleTestFixture.FrameBuilder(battle).Interaction.SetRangeEnemy(enemyId);
		Assert.True(BattleTestCommands.FireLightningCannon(battle));

		var frame = BattleTestCommands.Frame(battle);

		Assert.False(frame.PreviewUnits[enemyId].IsAlive);
		Assert.True(battle.Engine.World.StateOf(enemyId).IsAlive);
		Assert.Equal(enemyId, frame.RangeEnemy!.Value.EnemyId);
		Assert.Equal(enemyId, BattleTestFixture.FrameBuilder(battle).Interaction.RangeEnemyId);
	}

	[Fact]
	public void QueuedHitOnReticleTargetPersistsUntilUndo()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var simEnemy = battle.PlayerAgent.Sim.StateOf<ActorState>(enemyId);
		simEnemy.CurrentHullPoints = 1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			simEnemy.CurrentShieldPoints[face] = 0;

		BattleTestFixture.FrameBuilder(battle).Interaction.SetRangeEnemy(enemyId);
		Assert.True(BattleTestCommands.FireLightningCannon(battle));

		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(EPlayerMode.Move, frame.Mode);
		var damage = frame.AbilityDamage[enemyId];
		Assert.True(damage.IsLethal);
		Assert.Equal(1, damage.HullBefore);
		Assert.Equal(1, damage.HullDamage);

		Assert.True(battle.PlayerAgent.Undo());
		Assert.Empty(BattleTestCommands.Frame(battle).AbilityDamage);
	}

	[Fact]
	public void BattleOverFrameHidesRangeTarget()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(1, 1, 1));

		battle.ForceOutcome(EBattleResult.Win);

		Assert.Null(BattleTestCommands.Frame(battle).RangeEnemy);
	}

	private static BattleOrchestrator CreateOrchestrator(Coord playerPos, Coord enemyPos)
	{
		var encounter = new BattleEncounter
		{
			Id = "test-encounter",
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				BattleSpawnTestKit.Create(
					"player",
					EType.Fighter,
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

		return BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
	}
}
