using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Units;

[BattleTestSuite]
public sealed class HeatStateTests
{
	[Theory]
	[InlineData(EType.Fighter, true, 100, 20, 1f)]
	[InlineData(EType.Carrier, true, 100, 20, 1f)]
	[InlineData(EType.VoidBomb, false, null, 0, 1f)]
	public void BattleStateInitializesHeatFromChassis(
		EType chassis,
		bool participates,
		int? expectedCapacity,
		int expectedClearPerTurn,
		float expectedGainMultiplier)
	{
		var spec = ShipCatalog.SpecFor(chassis);
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog($"test-{chassis}", chassis),
			new Coord(5, 5, 5));

		Assert.Same(spec.Heat, state.HeatProfile);
		Assert.Equal(participates, state.HeatProfile.Participates);
		Assert.Equal(expectedCapacity, state.HeatProfile.Capacity);
		Assert.Equal(expectedClearPerTurn, state.HeatProfile.ClearPerTurn);
		Assert.Equal(expectedGainMultiplier, state.HeatProfile.GainMultiplier);
		Assert.Equal(0, state.Heat);
		Assert.False(state.Overdrive);
		Assert.False(state.Overheat);
	}

	[Fact]
	public void CloneSharesProfileButCopiesHeatFlags()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter", EType.Fighter),
			new Coord(5, 5, 5));
		state.Heat = 40;
		state.Overdrive = true;
		state.Overheat = false;

		var clone = state.Clone();
		clone.Heat = 0;
		clone.Overdrive = false;
		clone.Overheat = true;

		Assert.Same(state.HeatProfile, clone.HeatProfile);
		Assert.Equal(40, state.Heat);
		Assert.True(state.Overdrive);
		Assert.False(state.Overheat);
		Assert.Equal(0, clone.Heat);
		Assert.False(clone.Overdrive);
		Assert.True(clone.Overheat);
	}

	[Fact]
	public void DisplayProjectionPreservesHeatProfileAndFlags()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("carrier", EType.Carrier),
			new Coord(5, 5, 5));
		state.Heat = 55;
		state.Overdrive = true;
		state.Overheat = false;

		var display = UnitDisplayState.Capture(
			state,
			GrimSpace.Battle.Runtime.ActorRuntime.From(null));
		var restored = display.ToState();

		Assert.Same(state.HeatProfile, display.HeatProfile);
		Assert.Same(display.HeatProfile, restored.HeatProfile);
		Assert.Equal(55, display.Heat);
		Assert.True(display.Overdrive);
		Assert.False(display.Overheat);
		Assert.Equal(100, display.HeatCapacity);
		Assert.Equal(55, restored.Heat);
		Assert.True(restored.Overdrive);
		Assert.False(restored.Overheat);
	}

	[Fact]
	public void VoidBombDisplayHasNoHeatCapacity()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("bomb", EType.VoidBomb),
			new Coord(5, 5, 5));

		var display = UnitDisplayState.Capture(
			state,
			GrimSpace.Battle.Runtime.ActorRuntime.From(null));

		Assert.Same(HeatSpec.None, display.HeatProfile);
		Assert.False(display.HeatProfile.Participates);
		Assert.Equal(0, display.HeatCapacity);
	}
}
