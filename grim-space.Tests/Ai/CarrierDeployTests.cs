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

[BattleTestSuite]
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

		Assert.Contains(actions, action => action is SpawnRepurposedMinerAction);
	}

	[Fact]
	public async Task BuildTurnActions_SkipsDeployWhileCooldownActive()
	{
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(new Coord(5, 5, 5));
		carrier.State.ActionPoints = 0;
		StateMountTestKit.SetCooldownRemaining(carrier.State, GrimSpace.Units.Loadouts.Abilities.EAbilityKind.MinerBay, 1);

		var battle = BattleTestFixture.BeginSimulation(player, carrier);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, carrier);

		Assert.DoesNotContain(actions, action => action is SpawnRepurposedMinerAction);
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

		Assert.DoesNotContain(actions, action => action is SpawnRepurposedMinerAction);
	}

	[Fact]
	public async Task BuildTurnActions_SkipsDeployAtLivingCap()
	{
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(new Coord(5, 5, 5));
		carrier.State.ActionPoints = 0;

		var battle = BattleTestFixture.BeginSimulation(player, carrier);
		FillLivingRepurposedMiners(
			battle.Engine.World,
			carrier.State.Id,
			CatalogExpectations.DefaultMinerBaySpec().MaxLivingChildren);

		var actions = await BattleTestFixture.AwaitUnitActions(battle, carrier);

		Assert.DoesNotContain(actions, action => action is SpawnRepurposedMinerAction);
	}

	private static void FillLivingRepurposedMiners(BattleWorld world, string carrierId, int count)
	{
		var carrier = UnitRegistry.For(world).UnitOf(carrierId);
		for (var i = 0; i < count; i++)
		{
			var repurposedMiner = Factory.Create(
				ShipInstance.FromCatalog($"repurposed-miner-{i}", EType.RepurposedMiner),
				carrier.Team,
				new Coord(1 + i, 1, 5),
				new AiController(),
				Coord.Forward,
				Coord.Up,
				parentId: carrierId);
			UnitRegistry.For(world).Add(repurposedMiner);
		}
	}
}
