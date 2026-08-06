using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class RailgunActionTests
{
	private const string PlayerId = "player";

	private static int TotalShieldPoints(GrimSpace.Battle.Units.State state)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += state.ShieldPoints[face];
		return total;
	}

	[Fact]
	public void RailgunAppliesResolveImmediately()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			playerPos, TurnOrchestrationTests.EnemyInRailgunLine(playerPos));
		var shieldsBefore = TotalShieldPoints(battle.Sim.StateOf<ActorState>(battle.OpponentId));

		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(PlayerId)));
		Assert.True(shieldsBefore > TotalShieldPoints(battle.Sim.StateOf<ActorState>(battle.OpponentId)));
	}

	[Fact]
	public void RailgunPossibleWhenBurstMissesOpponent()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, new Coord(0, 0, 0));
		var action = new RailgunAction(PlayerId);

		Assert.True(RailgunDef.Instance.IsPossible(action, battle.Sim.World, battle.Sim.RuntimeFor(PlayerId)));
		Assert.True(battle.Sim.TryEnqueue(action));
	}

	[Fact]
	public void ResolveTurnAppliesRailgunDamageToEnemyInBurst()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 6;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var shieldsBefore = TotalShieldPoints(battle.Sim.StateOf<ActorState>(battle.OpponentId));
		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(PlayerId)));

		var replay = battle.ResolveTurn();

		Assert.Contains(replay.Actions, action => action is RailgunAction);
		Assert.True(shieldsBefore > TotalShieldPoints(battle.Engine.World.StateOf(battle.OpponentId)));
	}
}
