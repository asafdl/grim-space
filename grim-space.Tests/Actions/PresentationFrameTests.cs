using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
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

namespace GrimSpace.Tests.Actions;

public sealed class PresentationFrameTests
{
	[Fact]
	public void FrameAfterQueuedMoveShowsReachableExtensions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
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
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
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
	public void UndoClearsQueuedRailgun()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.Equal(0, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Single(battle.PlayerAgent.Sim.Actions);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	[Fact]
	public void UndoClearsQueuedFlak()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		Assert.True(BattleTestCommands.FireFlak(battle, ESpatialOrientation.Port));
		Assert.Equal(0, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(CombatConfig.FlaksPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).FlakRemaining);
	}

	[Fact]
	public void UndoClearsRailgunQueuedAfterMove()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.Equal(4, battle.PlayerAgent.Sim.Actions.Count);

		Assert.True(BattleTestCommands.Undo(battle));

		Assert.DoesNotContain(battle.PlayerAgent.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
		Assert.Equal(3, battle.PlayerAgent.Sim.Actions.Count(action => action is MoveStepAction));
	}

	[Fact]
	public void UndoRailgunPreservesCommittedMoveCheckpoints()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var threeStepEnd = origin + Coord.Forward * 3;

		Assert.True(BattleTestCommands.Move(battle, threeStepEnd));
		var preview = new PlanningPreview();
		var pathAfterMove = preview.CommittedMoveCheckpoints(battle.PlayerAgent.Sim, battle.PlayerId).ToList();
		Assert.NotEmpty(pathAfterMove);

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Equal(pathAfterMove, preview.CommittedMoveCheckpoints(battle.PlayerAgent.Sim, battle.PlayerId));
		Assert.DoesNotContain(battle.PlayerAgent.Sim.Actions, action => action is RailgunAction);
		Assert.Equal(CombatConfig.RailgunsPerTurn, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).RailgunRemaining);
	}

	[Fact]
	public void DefaultFocusIsPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

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
		var enemyPos = TurnOrchestrationTests.EnemyInRailgunLine(origin);
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
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
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
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var afterMove = origin + Coord.Forward * 2;

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.True(BattleTestCommands.Move(battle, afterMove));
		Assert.True(BattleTestCommands.FireFlak(battle, ESpatialOrientation.Port));

		var preview = new PlanningPreview();
		Assert.Equal(
			afterMove,
			preview.PreviewUnits(battle.PlayerAgent.Sim, battle.PlayerId)[battle.PlayerId].Position);

		var frame = BattleTestCommands.Frame(battle);
		var railgunPreview = Assert.Single(
			frame.AreaActions.Queued,
			preview => preview.Action is RailgunAction);
		var flakPreview = Assert.Single(
			frame.AreaActions.Queued,
			preview => preview.Action is FlakAction);
		var railgunIndex = battle.PlayerAgent.Sim.Actions
			.ToList()
			.FindIndex(action => action is RailgunAction);
		var flakIndex = battle.PlayerAgent.Sim.Actions
			.ToList()
			.FindIndex(action => action is FlakAction);
		var expectedRailgun = RailgunDef.Instance.AffectedCells(
			(RailgunAction)railgunPreview.Action,
			battle.PlayerAgent.Sim.ReplayWorld(railgunIndex));
		var expectedFlak = FlakDef.Instance.AffectedCells(
			(FlakAction)flakPreview.Action,
			battle.PlayerAgent.Sim.ReplayWorld(flakIndex));
		Assert.True(expectedRailgun.SetEquals(railgunPreview.Volume.Cells));
		Assert.True(expectedFlak.SetEquals(flakPreview.Volume.Cells));
		Assert.Equal(origin, railgunPreview.Volume.Origin);
		Assert.Equal(afterMove, flakPreview.Volume.Origin);
	}

	[Fact]
	public void FramePublishesAuthoritativeBoardClippedAreaActions()
	{
		var origin = new Coord(10, 10, 10);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		var frame = BattleTestCommands.Frame(battle);
		var railgunPreview = Assert.Single(
			frame.AreaActions.Aim,
			preview => preview.Action is RailgunAction);
		var expectedRailgun = RailgunDef.Instance.AffectedCells(
			(RailgunAction)railgunPreview.Action,
			battle.PlayerAgent.Sim.World);
		Assert.True(expectedRailgun.SetEquals(railgunPreview.Volume.Cells));
		Assert.Equal(origin, railgunPreview.Volume.Origin);
		var flakPreviews = frame.AreaActions.Aim
			.Where(preview => preview.Action is FlakAction)
			.ToDictionary(
				preview => ((FlakAction)preview.Action).MountedOn,
				preview => preview.Volume);
		Assert.Equal(
			frame.Weapons.PortFlak,
			flakPreviews.ContainsKey(ESpatialOrientation.Port));
		Assert.Equal(
			frame.Weapons.StarboardFlak,
			flakPreviews.ContainsKey(ESpatialOrientation.Starboard));
		foreach (var (mountedOn, volume) in flakPreviews)
		{
			var expectedFlak = FlakDef.Instance.AffectedCells(
				new FlakAction(battle.PlayerId, mountedOn),
				battle.PlayerAgent.Sim.World);
			Assert.True(expectedFlak.SetEquals(volume.Cells));
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
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var world = battle.PlayerAgent.Sim.World;
		var actor = world.StateOf(battle.PlayerId);
		actor.Fore = new Coord(1, 0, 0);
		actor.Dorsal = Coord.Up;
		actor.Starboard = Coord.Cross(actor.Dorsal, actor.Fore);
		var frame = BodyFrame.From(actor);
		var railgun = new RailgunAction(battle.PlayerId);
		var flak = new FlakAction(battle.PlayerId, ESpatialOrientation.Port);

		var railgunCells = ((IAreaActionDef)railgun.Definition).AffectedCells(railgun, world);
		var flakCells = ((IAreaActionDef)flak.Definition).AffectedCells(flak, world);

		Assert.Contains(frame.ToWorld(1, 0, 0), railgunCells);
		Assert.DoesNotContain(frame.Origin, railgunCells);
		Assert.Contains(frame.ToWorld(0, 1, 0), flakCells);
		Assert.DoesNotContain(frame.ToWorld(0, -1, 0), flakCells);
	}

	[Fact]
	public void QueuedTorpedoShowsSpawnedUnitWithoutTravelPreview()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			TorpedoDef.Instance.Bind(battle.PlayerId, ESpatialOrientation.Retro)));

		var action = Assert.IsType<TorpedoAction>(Assert.Single(battle.PlayerAgent.Sim.Actions));
		Assert.NotNull(action.SpawnedUnitId);
		var frame = BattleTestCommands.Frame(battle);

		Assert.Null(frame.TorpedoPreviews.Queued);
		Assert.Contains(
			frame.PreviewUnits.Values,
			unit => unit.Type == EType.Torpedo);
	}

	[Fact]
	public void HoveredTorpedoPublishesDisjointTurnVolumes()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, new Coord(0, 0, 0));
		var frames = BattleTestFixture.FrameBuilder(battle);
		var spec = AbilityHudCatalog.ForUnit(
				battle.PlayerAgent.Sim.World.StateOf(battle.PlayerId).Type)
			.First(entry => entry.Mode == EPlayerMode.Torpedo);
		frames.Interaction.SetMode(EPlayerMode.Torpedo, spec);
		var choices = BattleTestCommands.Frame(battle).AbilityChoices;
		var dorsalIndex = choices
			.Select((choice, index) => (choice, index))
			.Single(entry => entry.choice.MountedOn == ESpatialOrientation.Dorsal)
			.index;
		frames.Interaction.SetAbilityHover(dorsalIndex, choices.Count);

		var frame = BattleTestCommands.Frame(battle);
		var aim = frame.TorpedoPreviews.Aim;

		Assert.NotNull(aim);
		Assert.Null(frame.TorpedoPreviews.Queued);
		Assert.Equal(TorpedoConfig.Fuel, aim.TurnBands.Count);
		Assert.Equal(
			TorpedoMount.LaunchPose(
				battle.PlayerAgent.Sim.World.StateOf(battle.PlayerId),
				ESpatialOrientation.Dorsal).Position,
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
			.First(entry => entry.Mode == EPlayerMode.Torpedo);
		frames.Interaction.SetMode(EPlayerMode.Torpedo, spec);
		var choices = BattleTestCommands.Frame(battle).AbilityChoices;
		var retroIndex = choices
			.Select((choice, index) => (choice, index))
			.Single(entry => entry.choice.MountedOn == ESpatialOrientation.Retro)
			.index;
		frames.Interaction.SetAbilityHover(retroIndex, choices.Count);
		var aim = BattleTestCommands.Frame(battle).TorpedoPreviews.Aim;
		Assert.NotNull(aim);
		var firstLayer = aim.TurnBands[0];

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			TorpedoDef.Instance.Bind(battle.PlayerId, ESpatialOrientation.Retro)));

		var replay = BattleTestActions.CommitAndResolve(battle);
		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Torpedo);

		Assert.Contains(
			replay.Actions,
			action => action is TorpedoMoveStepAction { ActorId: var id } && id == torpedo.State.Id);
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
	public void TorpedoMountsExcludeBlockedLaunchCells()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var (blockedMount, _, _) = TorpedoMount.LaunchPose(
			player.State,
			ESpatialOrientation.Dorsal);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position, blockedMount });

		var mounts = BattleTestCommands.Frame(battle).Weapons.TorpedoMounts;

		Assert.DoesNotContain(ESpatialOrientation.Dorsal, mounts);
		Assert.Contains(ESpatialOrientation.Retro, mounts);
		Assert.Contains(ESpatialOrientation.Ventral, mounts);
	}

	[Fact]
	public void ThreatenedUnitIdsIncludesTargetsFromEveryQueuedWeapon()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var preview = new PlanningPreview();

		Assert.True(BattleTestCommands.FireRailgun(battle));
		Assert.Contains(
			enemyId,
			preview.ThreatenedUnitIds(battle.PlayerAgent.Sim, battle.PlayerId));

		Assert.True(BattleTestCommands.FireFlak(battle, ESpatialOrientation.Port));

		Assert.Contains(
			enemyId,
			preview.ThreatenedUnitIds(battle.PlayerAgent.Sim, battle.PlayerId));
	}

	[Fact]
	public void PreviewRetainsPredictedDeadUnits()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var enemy = battle.PlayerAgent.Sim.StateOf<ActorState>(enemyId);
		enemy.HullPoints = 1;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			enemy.ShieldPoints[face] = 0;

		Assert.True(BattleTestCommands.FireRailgun(battle));

		var preview = new PlanningPreview().PreviewUnits(battle.PlayerAgent.Sim, battle.PlayerId);

		Assert.True(preview.ContainsKey(enemyId));
		Assert.False(preview[enemyId].IsAlive);
	}

	[Fact]
	public void InvalidFocusTargetFallsBackToPlayer()
	{
		var origin = new Coord(5, 5, 5);
		var battle = CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		BattleTestCommands.Focus(battle, "missing");
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal(battle.PlayerId, frame.FocusId);
		Assert.False(frame.IsInspecting);
		Assert.Null(BattleTestFixture.FrameBuilder(battle).Interaction.FocusId);
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
