using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public readonly record struct UnitCombatSnapshot(
	int HullPoints,
	FaceShieldPoints ShieldPoints,
	bool ApPenaltyNextTurn)
{
	public static UnitCombatSnapshot Capture(State unit) =>
		new(unit.HullPoints, unit.ShieldPoints.Clone(), unit.ApPenaltyNextTurn);

	public void Restore(State unit)
	{
		unit.HullPoints = HullPoints;
		unit.ShieldPoints = ShieldPoints.Clone();
		unit.ApPenaltyNextTurn = ApPenaltyNextTurn;
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
