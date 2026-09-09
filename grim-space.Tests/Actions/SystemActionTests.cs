using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Ids;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class SystemActionTests
{
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
		Assert.Equal(BattleActorIds.Terrain, battle.Engine.World.NonUnits[asteroid.Id].ActorId);
	}
}
