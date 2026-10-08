using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class ManeuverabilityStateTests
{
	[Theory]
	[InlineData(EType.Fighter, 4, 3)]
	[InlineData(EType.Carrier, 3, 1)]
	[InlineData(EType.RepurposedMiner, 4, 2)]
	[InlineData(EType.VoidBomb, 4, 0)]
	public void BattleStateInitializesResourcesFromChassis(
		EType chassis,
		int expectedActionPoints,
		int expectedManeuverPoints)
	{
		var spec = ShipCatalog.SpecFor(chassis);
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog($"test-{chassis}", chassis),
			new Coord(5, 5, 5));

		Assert.Same(spec.Maneuverability, state.Maneuverability);
		Assert.Equal(expectedActionPoints, state.ActionPoints);
		Assert.Equal(expectedManeuverPoints, state.ManeuverPoints);
	}

	[Fact]
	public void CloneSharesProfileButCopiesRemainingResources()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter", EType.Fighter),
			new Coord(5, 5, 5));
		state.ActionPoints = 2;
		state.ManeuverPoints = 1;

		var clone = state.Clone();
		clone.ActionPoints = 0;
		clone.ManeuverPoints = 0;

		Assert.Same(state.Maneuverability, clone.Maneuverability);
		Assert.Equal(2, state.ActionPoints);
		Assert.Equal(1, state.ManeuverPoints);
	}

	[Fact]
	public void DisplayProjectionPreservesEffectiveProfileAndResources()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("carrier", EType.Carrier),
			new Coord(5, 5, 5));
		state.ActionPoints = 2;
		state.ManeuverPoints = 0;

		var display = UnitDisplayState.Capture(state);
		var restored = display.ToState();

		Assert.Same(state.Maneuverability, display.Maneuverability);
		Assert.Same(display.Maneuverability, restored.Maneuverability);
		Assert.Equal(2, restored.ActionPoints);
		Assert.Equal(0, restored.ManeuverPoints);
		Assert.Equal(3, display.MaxActionPoints);
		Assert.Equal(1, display.MaxManeuverPoints);
	}
}
