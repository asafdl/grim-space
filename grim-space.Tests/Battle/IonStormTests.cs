using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Hazards;

[BattleTestSuite]
public sealed class IonStormTests
{
	private const string PlayerId = "player";
	private static readonly Coord Origin = new(5, 5, 5);
	private static readonly Coord StormCell = Origin + Coord.Up;

	[Fact]
	public void EnteringStormGrantsShieldsAndForcesOverdriveAndUndoRestoresState()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var actor = world.StateOf(PlayerId);
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));
		actor.HeatProfile = new HeatSpec(100, 20, 0.1f);
		actor.Heat = 10;
		TemporaryShields.GrantPerFace(actor, "ability", 3);
		var move = new MoveEffect(StormCell);

		var records = move.Apply(world, runtime, PlayerId);

		Assert.Equal(100, actor.Heat);
		Assert.True(actor.Overdrive);
		Assert.False(actor.Overheat);
		Assert.Equal(1, world.Timeline.CountPending(a => a is EnterOverheatAction));
		Assert.Contains(records, record => record is Record<ShieldsChangedFacts>);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			var expected = (int)System.Math.Round(actor.Loadout.BaseShieldPoints[face] * 0.25m, MidpointRounding.AwayFromZero);
			Assert.Equal(3 + expected, TemporaryShields.TotalOnFace(actor, face));
		}

		move.Undo(world, runtime, PlayerId);

		Assert.Equal(Origin, actor.Position);
		Assert.Equal(10, actor.Heat);
		Assert.False(actor.Overdrive);
		Assert.Equal(0, world.Timeline.CountPending(a => a is EnterOverheatAction));
		Assert.Equal("ability", Assert.Single(TemporaryShields.ExtractGrants(actor)).GrantId);
	}

	[Fact]
	public void ReenteringRefreshesOnlyStormShieldsWithoutDuplicateOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var actor = world.StateOf(PlayerId);
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));
		new MoveEffect(StormCell).Apply(world, runtime, PlayerId);
		var face = ESpatialOrientation.Forward;
		var initial = TemporaryShields.TotalOnFace(actor, face);
		Assert.True(initial > 0);
		Assert.Equal(1, TemporaryShields.AbsorbOnFace(actor, face, 1));
		TemporaryShields.GrantPerFace(actor, "ability", 3);
		new HeatChangeEffect(-30).Apply(world, runtime, PlayerId);
		new MoveEffect(Origin).Apply(world, runtime, PlayerId);
		var grantOrder = TemporaryShields.ExtractGrants(actor).Select(grant => grant.GrantId).ToArray();

		var reentry = new MoveEffect(StormCell);
		var records = reentry.Apply(world, runtime, PlayerId);

		Assert.Equal(initial + 3, TemporaryShields.TotalOnFace(actor, face));
		Assert.Equal(2, TemporaryShields.ExtractGrants(actor).Count);
		Assert.Equal(grantOrder, TemporaryShields.ExtractGrants(actor).Select(grant => grant.GrantId));
		Assert.Single(records.OfType<Record<ShieldsChangedFacts>>());
		Assert.Equal(100, actor.Heat);
		Assert.Equal(1, world.Timeline.CountPending(a => a is EnterOverheatAction));

		reentry.Undo(world, runtime, PlayerId);
		Assert.Equal(initial + 2, TemporaryShields.TotalOnFace(actor, face));
		Assert.Equal(70, actor.Heat);
		Assert.True(actor.Overdrive);
		Assert.Equal(1, world.Timeline.CountPending(a => a is EnterOverheatAction));
	}

	[Fact]
	public void MovementHeatOnReentryDoesNotScheduleAnotherOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var actor = world.StateOf(PlayerId);
		var cell = Origin + actor.Fore;
		world.AddNonUnit(IonStorm.Create("ionstorm-a", cell, world.Grid));
		new MoveEffect(cell).Apply(world, runtime, PlayerId);
		new HeatChangeEffect(-5).Apply(world, runtime, PlayerId);
		new MoveEffect(Origin).Apply(world, runtime, PlayerId);

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward), world, runtime, PlayerId));

		Assert.Equal(cell, actor.Position);
		Assert.Equal(1, world.Timeline.CountPending(a => a is EnterOverheatAction));
	}

	[Fact]
	public void StayingInStormRepeatsHeatCycleWithoutRefreshingShields()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var actor = world.StateOf(PlayerId);
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));
		new MoveEffect(StormCell).Apply(world, runtime, PlayerId);
		var face = ESpatialOrientation.Forward;
		var initial = TemporaryShields.TotalOnFace(actor, face);
		Assert.True(initial > 0);
		TemporaryShields.AbsorbOnFace(actor, face, 1);

		battle.Engine.AdvanceTick();
		Assert.True(actor.Overheat);
		Assert.False(actor.Overdrive);
		battle.Engine.AdvanceTick();

		Assert.False(actor.Overheat);
		Assert.True(actor.Overdrive);
		Assert.Equal(actor.HeatProfile.EffectiveCapacity, actor.Heat);
		Assert.Equal(initial - 1, TemporaryShields.TotalOnFace(actor, face));
		Assert.Equal(1, world.Timeline.CountPending(a => a is EnterOverheatAction));
	}

	[Fact]
	public void EnteringWhileOverheatedGrantsShieldsButWaitsForOverheatToClear()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var actor = world.StateOf(PlayerId);
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));
		new HeatChangeEffect(100).Apply(world, runtime, PlayerId);
		battle.Engine.AdvanceTick();
		Assert.True(actor.Overheat);

		new MoveEffect(StormCell).Apply(world, runtime, PlayerId);

		Assert.True(actor.Overheat);
		Assert.False(actor.Overdrive);
		Assert.True(TemporaryShields.TotalOnFace(actor, ESpatialOrientation.Forward) > 0);
		battle.Engine.AdvanceTick();
		Assert.False(actor.Overheat);
		Assert.True(actor.Overdrive);
		Assert.Equal(actor.HeatProfile.EffectiveCapacity, actor.Heat);
	}

	[Fact]
	public void ShipsWithoutHeatReceiveNoStormEffects()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var actor = world.StateOf(PlayerId);
		actor.HeatProfile = HeatSpec.None;
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));

		new MoveEffect(StormCell).Apply(world, battle.Engine.ActorRuntimes.For(PlayerId), PlayerId);

		Assert.Empty(TemporaryShields.ExtractGrants(actor));
		Assert.False(actor.Overdrive);
		Assert.Equal(0, actor.Heat);
	}

	[Fact]
	public void LeavingDuringOverheatPreventsAnotherStormOverdrive()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var actor = world.StateOf(PlayerId);
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));
		new MoveEffect(StormCell).Apply(world, runtime, PlayerId);
		battle.Engine.AdvanceTick();
		Assert.True(actor.Overheat);

		new MoveEffect(Origin).Apply(world, runtime, PlayerId);
		battle.Engine.AdvanceTick();

		Assert.False(actor.Overheat);
		Assert.False(actor.Overdrive);
		Assert.Equal(0, actor.Heat);
		Assert.Equal(0, world.Timeline.CountPending(a => a is EnterOverheatAction));
	}

	[Fact]
	public void MovePreviewIncludesStormEffectsWithoutChangingLiveState()
	{
		var grid = BattleTestFixture.Grid(20);
		var player = BattleTestFixture.Player(Origin);
		var cell = Origin + player.State.Fore;
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[BattleTestFixture.Enemy(Coord.Zero)],
			grid,
			hazards: [IonStorm.Create("ionstorm-a", cell, grid)]);

		var result = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId)
			.First(path => path.EndPosition == cell).ResultState;

		Assert.True(result.Overdrive);
		Assert.Equal(result.HeatProfile.EffectiveCapacity, result.Heat);
		Assert.True(TemporaryShields.TotalOnFace(result, ESpatialOrientation.Forward) > 0);
		Assert.Equal(0, player.State.Heat);
		Assert.Empty(TemporaryShields.ExtractGrants(player.State));
	}

	[Fact]
	public void StormAndGrantedShieldsSurviveBattleWorldSave()
	{
		var battle = BattleTestFixture.BeginSimulation(Origin);
		var world = battle.Engine.World;
		var actor = world.StateOf(PlayerId);
		world.AddNonUnit(IonStorm.Create("ionstorm-a", StormCell, world.Grid));
		new MoveEffect(StormCell).Apply(world, battle.Engine.ActorRuntimes.For(PlayerId), PlayerId);
		var expected = TemporaryShields.TotalOnFace(actor, ESpatialOrientation.Forward);
		var registry = PersistenceRegistry.CreateDefault();

		var restored = SaveDtoMapper.RestoreBattleWorld(SaveDtoMapper.CaptureBattleWorld(world, registry), registry);

		Assert.NotNull(restored.IonStormAt(StormCell));
		Assert.Equal(expected, TemporaryShields.TotalOnFace(restored.StateOf(PlayerId), ESpatialOrientation.Forward));
		Assert.True(restored.StateOf(PlayerId).Overdrive);
		Assert.Equal(1, restored.Timeline.CountPending(a => a is EnterOverheatAction));
	}
}
