using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

public sealed class ShipGearTierTintTests
{
	[Fact]
	public void TryGet_MapsT1T2AndT3_Only()
	{
		Assert.True(ShipGearTierTint.TryGet(EShipGearTier.T1, out var t1));
		Assert.Equal(ShipGearTierTint.T1, t1);
		Assert.True(ShipGearTierTint.TryGet(EShipGearTier.T2, out var t2));
		Assert.Equal(ShipGearTierTint.T2, t2);
		Assert.True(ShipGearTierTint.TryGet(EShipGearTier.T3, out var t3));
		Assert.Equal(ShipGearTierTint.T3, t3);
		Assert.False(ShipGearTierTint.TryGet(EShipGearTier.T0, out _));
		Assert.Equal(Colors.White, ShipGearTierTint.ModulateFor(EShipGearTier.T0));
		Assert.Equal(ShipGearTierTint.T1, ShipGearTierTint.ModulateFor(EShipGearTier.T1));
	}
}
