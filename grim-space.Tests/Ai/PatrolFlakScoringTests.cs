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

public sealed class PatrolFlakScoringTests
{
	[Fact]
	public async Task BuildTurnActions_FiresFlakWhenBurstHitsPlayer()
	{
		var grid = BattleTestFixture.Grid();
		var patrolPos = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var patrol = BattleTestFixture.Patrol(patrolPos);
		patrol.State.ActionPoints = 0;
		patrol.State.Fore = new Coord(1, 0, 0);
		patrol.State.Dorsal = Coord.Up;
		patrol.State.Starboard = Coord.Cross(patrol.State.Dorsal, patrol.State.Fore);

		var frame = BodyFrame.From(patrol.State);
		var burstCells = WeaponBursts.FlakBurstCells(
			frame,
			FlakMountConfig.For(EFlakMount.Port),
			grid.IsInBounds);
		Assert.NotEmpty(burstCells);
		player.State.Position = burstCells.First();

		var battle = BattleTestFixture.BeginSimulation(player, patrol, grid);
		battle.SetActive(null);
		battle.SetActive(patrol.State.Id);
		var actions = await patrol.ExecutionAgent.GetActions();

		Assert.Contains(actions, action => action is FlakAction);
	}

	[Fact]
	public async Task BuildTurnActions_DoesNotFireFlakWhenBurstMissesPlayer()
	{
		var patrolPos = new Coord(8, 5, 5);
		var player = BattleTestFixture.Player(new Coord(2, 5, 5));
		var patrol = BattleTestFixture.Patrol(patrolPos);
		patrol.State.ActionPoints = 0;
		patrol.State.Fore = Coord.Forward;
		patrol.State.Dorsal = Coord.Up;
		patrol.State.Starboard = Coord.Cross(patrol.State.Dorsal, patrol.State.Fore);

		var battle = BattleTestFixture.BeginSimulation(player, patrol);
		battle.SetActive(null);
		battle.SetActive(patrol.State.Id);
		var actions = await patrol.ExecutionAgent.GetActions();

		Assert.DoesNotContain(actions, action => action is FlakAction);
	}
}
