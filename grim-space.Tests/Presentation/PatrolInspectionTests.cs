using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

public sealed class PatrolInspectionTests
{
	[Fact]
	public void FocusPatrolShowsForwardShieldProfile()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(0, 5, 5)),
			BattleTestFixture.Patrol(new Coord(8, 5, 5), id: "patrol-a"));

		BattleTestCommands.Focus(battle, "patrol-a");
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal("patrol-a", frame.FocusId);
		Assert.Equal(EType.Patrol, frame.FocusState.Type);
		Assert.Equal(3, frame.FocusState.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, frame.FocusState.MaxShieldPoints[ESpatialOrientation.Retro]);
		Assert.Contains("patrol-a", frame.PreviewUnits.Keys);
	}
}
