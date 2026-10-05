using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem;

[StarSystemTestSuite]
public sealed class FleetTests
{
	[Fact]
	public void ConstructorRejectsDuplicateMemberIds()
	{
		var member = new FleetMember("repurposed-miner-one");

		Assert.Throws<ArgumentException>(() =>
			new Fleet(
				new State
				{
					Id = "pirate-fleet",
					Type = GrimSpace.World.StarSystem.Units.EType.PirateFleet,
				},
				[member, member]));
	}

	[Fact]
	public void FactoryRejectsAggressionOutsideRange()
	{
		foreach (var rating in new[] { -1, 11 })
			Assert.Throws<ArgumentOutOfRangeException>(() => Factory.Create(SpawnWithAggression(rating)));
	}

	[Fact]
	public void SpawnDefaultsAndCopiesAggressionToStateAndClone()
	{
		var defaultState = State.FromSpawn(SpawnWithAggression());
		var state = State.FromSpawn(SpawnWithAggression(7));

		Assert.Equal(0, defaultState.AggressionRating);
		Assert.Equal(7, state.AggressionRating);
		Assert.Equal(7, state.Clone().AggressionRating);
	}

	private static Spawn SpawnWithAggression(int aggressionRating = 0) =>
		new(
			"pirate-fleet",
			EType.PirateFleet,
			"",
			new Coord(0, 0, 0),
			5,
			6,
			120,
			[],
			AggressionRating: aggressionRating);
}
