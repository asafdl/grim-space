using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class CarrierDeployTests
{
	[Fact]
	public async Task BuildTurnActions_AppendsDeployWhenLegal()
	{
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(new Coord(5, 5, 5));
		carrier.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, carrier);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, carrier);

		Assert.Contains(actions, action => action is SpawnPatrolAction);
	}

	[Fact]
	public async Task BuildTurnActions_SkipsDeployWhileCooldownActive()
	{
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(new Coord(5, 5, 5));
		carrier.State.ActionPoints = 0;
		StateMountTestKit.SetCooldownRemaining(carrier.State, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.PatrolBay, 1);

		var battle = BattleTestFixture.BeginSimulation(player, carrier);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, carrier);

		Assert.DoesNotContain(actions, action => action is SpawnPatrolAction);
	}

	[Fact]
	public async Task BuildTurnActions_SkipsDeployWhenBayBlocked()
	{
		var carrierPos = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(carrierPos);
		carrier.State.ActionPoints = 0;

		var frame = BodyFrame.From(carrier.State);
		var blockedBay = carrierPos + frame.Step(ESpatialOrientation.Ventral);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			carrier,
			blocked: new HashSet<Coord> { carrierPos, blockedBay });

		var actions = await BattleTestFixture.AwaitUnitActions(battle, carrier);

		Assert.DoesNotContain(actions, action => action is SpawnPatrolAction);
	}

	[Fact]
	public async Task BuildTurnActions_SkipsDeployAtLivingCap()
	{
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(new Coord(5, 5, 5));
		carrier.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, carrier);
		FillLivingPatrols(
			battle.Engine.World,
			carrier.State.Id,
			CatalogExpectations.DefaultPatrolBaySpec().MaxLivingChildren);

		var actions = await BattleTestFixture.AwaitUnitActions(battle, carrier);

		Assert.DoesNotContain(actions, action => action is SpawnPatrolAction);
	}

	private static void FillLivingPatrols(BattleWorld world, string carrierId, int count)
	{
		var carrier = UnitRegistry.For(world).UnitOf(carrierId);
		for (var i = 0; i < count; i++)
		{
			var patrol = Factory.Create(
				ShipInstance.FromCatalog($"patrol-{i}", EType.Patrol),
				carrier.Team,
				new Coord(1 + i, 1, 5),
				new AiController(),
				Coord.Forward,
				Coord.Up,
				parentId: carrierId);
			UnitRegistry.For(world).Add(patrol);
		}
	}
}
