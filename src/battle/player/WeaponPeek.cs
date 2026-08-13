using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Player;

/// <summary>Published weapon availability for the current human planning snapshot.</summary>
public readonly record struct WeaponPeek(
	bool PortFlak,
	bool StarboardFlak,
	bool Railgun,
	IReadOnlySet<ESpatialOrientation> TorpedoMounts)
{
	public static WeaponPeek Empty { get; } =
		new(false, false, false, new HashSet<ESpatialOrientation>());

	public bool IsKindLegal(EWeaponKind kind) =>
		kind switch
		{
			EWeaponKind.Flak => PortFlak || StarboardFlak,
			EWeaponKind.Railgun => Railgun,
			EWeaponKind.Torpedo => TorpedoMounts.Count > 0,
			_ => false,
		};
}
