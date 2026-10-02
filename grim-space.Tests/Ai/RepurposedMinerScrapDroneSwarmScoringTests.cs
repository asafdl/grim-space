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
public sealed class RepurposedMinerScrapDroneSwarmScoringTests
{
	[Fact]
	public async Task BuildTurnActions_FiresScrapDroneSwarmWhenBurstHitsPlayer()
	{
		var grid = BattleTestFixture.Grid();
		var repurposedMinerPos = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var repurposedMiner = BattleTestFixture.RepurposedMiner(repurposedMinerPos);
		repurposedMiner.State.ActionPoints = 0;
		repurposedMiner.State.Fore = new Coord(1, 0, 0);
		repurposedMiner.State.Dorsal = Coord.Up;
		repurposedMiner.State.Starboard = Coord.Cross(repurposedMiner.State.Dorsal, repurposedMiner.State.Fore);

		var frame = BodyFrame.From(repurposedMiner.State);
		player.State.Position = frame.ToWorld(0, 1, 0);

		var battle = BattleTestFixture.BeginSimulation(player, repurposedMiner, grid);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, repurposedMiner);

		Assert.Contains(actions, action => action is ScrapDroneSwarmAction);
	}

	[Fact]
	public async Task BuildTurnActions_DoesNotFireScrapDroneSwarmWhenBurstMissesPlayer()
	{
		var repurposedMinerPos = new Coord(8, 5, 5);
		var player = BattleTestFixture.Player(new Coord(2, 5, 5));
		var repurposedMiner = BattleTestFixture.RepurposedMiner(repurposedMinerPos);
		repurposedMiner.State.ActionPoints = 0;
		repurposedMiner.State.Fore = Coord.Forward;
		repurposedMiner.State.Dorsal = Coord.Up;
		repurposedMiner.State.Starboard = Coord.Cross(repurposedMiner.State.Dorsal, repurposedMiner.State.Fore);

		var battle = BattleTestFixture.BeginSimulation(player, repurposedMiner);
		var actions = await BattleTestFixture.AwaitUnitActions(battle, repurposedMiner);

		Assert.DoesNotContain(actions, action => action is ScrapDroneSwarmAction);
	}
}
