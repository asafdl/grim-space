using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Units;

public readonly record struct UnitCombatSnapshot(
	int CurrentHullPoints,
	FaceShieldPoints CurrentShieldPoints,
	IReadOnlyList<TemporaryShieldGrant> TemporaryShieldGrants)
{
	public static UnitCombatSnapshot Capture(State unit) =>
		new(
			unit.CurrentHullPoints,
			unit.CurrentShieldPoints.Clone(),
			TemporaryShields.ExtractGrants(unit));

	public void Restore(State unit)
	{
		unit.CurrentHullPoints = CurrentHullPoints;
		unit.CurrentShieldPoints = CurrentShieldPoints.Clone();
		TemporaryShields.ReplaceGrants(unit, TemporaryShieldGrants);
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
