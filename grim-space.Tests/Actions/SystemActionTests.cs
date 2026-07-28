using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Core;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class SystemActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ResolveTurnClearsLeftoverTurnHazards()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var hazard = Hazard.FlakBurst(
			"flak-leftover",
			PlayerId,
			BodyFrame.WorldAligned(new Coord(1, 1, 1)),
			[new Coord(1, 1, 1)]);
		BattleTestWorld.InjectHazard(battle.Engine.World, hazard);
		Assert.Single(battle.Engine.World.TurnHazards);

		battle.ResolveTurn([]);

		Assert.Empty(battle.Engine.World.TurnHazards);
	}

	[Fact]
	public void ResolveTurnClearsScheduledTurnHazards()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		Assert.True(battle.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Port)));

		battle.ResolveTurn(battle.Sim.Actions.ToList());
		Assert.Empty(battle.Engine.World.TurnHazards);
	}

	[Fact]
	public void ResolveTurnPreservesWorldHazards()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		var asteroid = Hazard.Asteroid(
			"asteroid-1",
			new Coord(2, 2, 2),
			battle.Layout.Grid,
			radius: 1,
			visualId: "rock");
		BattleTestWorld.InjectHazard(battle.Engine.World, asteroid);

		battle.ResolveTurn([]);

		Assert.Contains(asteroid.Id, battle.Engine.World.NonUnits.Keys);
		Assert.Equal(EntityIds.World, battle.Engine.World.NonUnits[asteroid.Id].ActorId);
	}

	[Fact]
	public void ResolveHazardActionAppliesAtScheduledTick()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Starboard)));

		var resolveTick = battle.Sim.AnchorTick + CombatConfig.FlakResolveDelay;
		Assert.Single(battle.Sim.PeekTimeline(resolveTick).OfType<ResolveHazardAction>());

		BattleTestApply.AdvancePreviewToTick(battle, resolveTick);

		Assert.Empty(battle.Sim.PeekTimeline(resolveTick));
	}
}
