using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public readonly record struct UnitCombatSnapshot(
	int HullPoints,
	FaceShieldPoints ShieldPoints,
	TemporaryShieldGrant[] TemporaryShieldGrants)
{
	public static UnitCombatSnapshot Capture(State unit) =>
		new(
			unit.HullPoints,
			unit.ShieldPoints.Clone(),
			unit.TemporaryShieldGrants.Select(grant => grant.Clone()).ToArray());

	public void Restore(State unit)
	{
		unit.HullPoints = HullPoints;
		unit.ShieldPoints = ShieldPoints.Clone();
		unit.TemporaryShieldGrants.Clear();
		foreach (var grant in TemporaryShieldGrants)
			unit.TemporaryShieldGrants.Add(grant.Clone());
	}
}

public readonly record struct OrientationSnapshot(Coord Fore, Coord Dorsal, Coord Starboard)
{
	public static OrientationSnapshot Capture(State unit) => new(unit.Fore, unit.Dorsal, unit.Starboard);

	public void Restore(State unit)
	{
		unit.Fore = Fore;
		unit.Dorsal = Dorsal;
		unit.Starboard = Starboard;
	}
}
