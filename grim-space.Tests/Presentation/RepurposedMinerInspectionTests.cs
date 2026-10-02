using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class RepurposedMinerInspectionTests
{
	[Fact]
	public void FocusRepurposedMinerShowsForwardShieldProfile()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(0, 5, 5)),
			BattleTestFixture.RepurposedMiner(new Coord(8, 5, 5), id: "repurposed-miner-a"));

		BattleTestCommands.Focus(battle, "repurposed-miner-a");
		var frame = BattleTestCommands.Frame(battle);

		Assert.Equal("repurposed-miner-a", frame.FocusId);
		Assert.Equal(EType.RepurposedMiner, frame.FocusState.Type);
		Assert.Equal(3, frame.FocusState.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, frame.FocusState.Loadout.MaxShieldPoints[ESpatialOrientation.Retro]);
		Assert.Contains("repurposed-miner-a", frame.PreviewUnits.Keys);
	}
}
