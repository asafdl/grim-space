using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class LightningCannonActionTests
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
	public void LightningCannonAppliesResolveImmediately()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			playerPos, TurnOrchestrationTests.EnemyInLightningCannonLine(playerPos));
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));
		Assert.True(shieldsBefore > TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle))));
	}

	[Fact]
	public void LightningCannonPossibleWhenBurstMissesOpponent()
	{
		var playerPos = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, new Coord(0, 0, 0));
		var action = new LightningCannonAction(PlayerId);

		Assert.True(LightningCannonDef.Instance.IsPossible(action, battle.PlayerAgent.Sim.World, battle.PlayerAgent.Sim.RuntimeFor(PlayerId)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));
	}

	[Fact]
	public void ResolveTurnAppliesLightningCannonDamageToEnemyInBurst()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 6;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		var shieldsBefore = TotalShieldPoints(battle.PlayerAgent.Sim.StateOf<ActorState>(BattleTestFixture.FirstEnemyId(battle)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new LightningCannonAction(PlayerId)));

		var replay = BattleTestActions.CommitAndResolve(battle);

		Assert.Contains(replay.Actions, action => action is LightningCannonAction);
		Assert.True(shieldsBefore > TotalShieldPoints(battle.Engine.World.StateOf(BattleTestFixture.FirstEnemyId(battle))));
	}
}
