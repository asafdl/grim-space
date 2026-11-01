using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public readonly record struct UnitCombatSnapshot(
	int HullPoints,
	FaceShieldPoints ShieldPoints,
	IReadOnlyList<TemporaryShieldGrant> TemporaryShieldGrants,
	int DefensiveManeuversRollIndex)
{
	public static UnitCombatSnapshot Capture(State unit) =>
		new(
			unit.HullPoints,
			unit.ShieldPoints.Clone(),
			TemporaryShields.ExtractGrants(unit),
			unit.DefensiveManeuversRollIndex);

	public void Restore(State unit)
	{
		unit.HullPoints = HullPoints;
		unit.ShieldPoints = ShieldPoints.Clone();
		TemporaryShields.ReplaceGrants(unit, TemporaryShieldGrants);
		unit.DefensiveManeuversRollIndex = DefensiveManeuversRollIndex;
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
