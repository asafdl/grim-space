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
	public void RailgunSchedulesResolveOnPreviewTimeline()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(PlayerId)));

		var resolveTick = battle.Sim.AnchorTick + CombatConfig.RailgunResolveDelay;
		var hazard = Assert.Single(battle.Sim.PeekTimeline(resolveTick).OfType<ResolveHazardAction>());
		Assert.NotEmpty(hazard.Cells);
		Assert.Equal(CombatConfig.RailgunDamage, hazard.Damage);
		Assert.Equal(EHazardKind.RailgunBurst, hazard.Kind);
	}

	[Fact]
	public void ResolveTurnAppliesRailgunDamageToEnemyInBurst()
	{
		var playerPos = new Coord(5, 5, 5);
		var enemyPos = playerPos + Coord.Forward * 6;
		var battle = TurnOrchestrationTests.CreateOrchestrator(playerPos, enemyPos);
		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(PlayerId)));

		var shieldsBefore = TotalShieldPoints(battle.Sim.StateOf<ActorState>(battle.OpponentId));
		var replay = battle.ResolveTurn(battle.Sim.Actions.ToList());

		Assert.Contains(replay.AppliedActions, action => action is RailgunAction);
		Assert.Contains(replay.AppliedActions, action => action is ResolveHazardAction resolve
			&& resolve.Kind == EHazardKind.RailgunBurst);
		Assert.True(shieldsBefore > TotalShieldPoints(battle.Engine.World.StateOf(battle.OpponentId)));
	}
}
