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
		var member = new FleetMember("patrol-one");

		Assert.Throws<ArgumentException>(() =>
			new Fleet(
				new State
				{
					Id = "pirate-fleet",
					Type = GrimSpace.World.StarSystem.Units.EType.PirateFleet,
				},
				[member, member]));
	}
}
