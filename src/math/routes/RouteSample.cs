using GrimSpace.Math.Grid;

namespace GrimSpace.Math.Routes;

public readonly record struct RouteSample(
	double X,
	double Z,
	double TangentX,
	double TangentZ,
	int NextPointIndex)
{
	public (Coord Position, Coord Tangent) ToRoundedCoord() =>
		(
			new Coord((int)System.Math.Round(X), 0, (int)System.Math.Round(Z)),
			new Coord(
				(int)System.Math.Round(TangentX * 1000),
				0,
				(int)System.Math.Round(TangentZ * 1000)));
}
