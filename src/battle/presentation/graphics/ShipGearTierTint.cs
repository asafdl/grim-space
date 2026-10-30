using Godot;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Presentation-only hull tints for purchased gear tiers.</summary>
public static class ShipGearTierTint
{
	public static readonly Color T1 = new(0.42f, 1f, 0.52f);
	public static readonly Color T2 = new(0.38f, 0.62f, 1f);
	public static readonly Color T3 = new(1f, 0.82f, 0.28f);

	public static bool TryGet(EShipGearTier tier, out Color tint)
	{
		switch (tier)
		{
			case EShipGearTier.T1:
				tint = T1;
				return true;
			case EShipGearTier.T2:
				tint = T2;
				return true;
			case EShipGearTier.T3:
				tint = T3;
				return true;
			default:
				tint = default;
				return false;
		}
	}

	public static Color ModulateFor(EShipGearTier tier) =>
		TryGet(tier, out var tint) ? tint : Colors.White;
}
