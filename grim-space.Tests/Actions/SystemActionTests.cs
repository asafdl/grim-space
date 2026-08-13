using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Core;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
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

		BattleTestActions.CommitAndResolve(battle);

		Assert.Empty(battle.Engine.World.TurnHazards);
	}

	[Fact]
	public void ResolveTurnClearsScheduledTurnHazards()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new FlakAction(PlayerId, EFlakMount.Port)));

		BattleTestActions.CommitAndResolve(battle);
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
			[new Coord(2, 2, 2), new Coord(3, 2, 2)]);
		BattleTestWorld.InjectHazard(battle.Engine.World, asteroid);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Contains(asteroid.Id, battle.Engine.World.NonUnits.Keys);
		Assert.Equal(EntityIds.World, battle.Engine.World.NonUnits[asteroid.Id].ActorId);
	}
}
