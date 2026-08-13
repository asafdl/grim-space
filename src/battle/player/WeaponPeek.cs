using GrimSpace.Battle.Abilities;

namespace GrimSpace.Battle.Player;

/// <summary>Published weapon availability for the current human planning snapshot.</summary>
public readonly record struct WeaponPeek(
	bool PortFlak,
	bool StarboardFlak,
	bool Railgun,
	IReadOnlySet<ETorpedoMount> TorpedoMounts)
{
	public static WeaponPeek Empty { get; } =
		new(false, false, false, new HashSet<ETorpedoMount>());

	public bool IsKindLegal(EWeaponKind kind) =>
		kind switch
		{
			EWeaponKind.Flak => PortFlak || StarboardFlak,
			EWeaponKind.Railgun => Railgun,
			EWeaponKind.Torpedo => TorpedoMounts.Count > 0,
			_ => false,
		};
}
