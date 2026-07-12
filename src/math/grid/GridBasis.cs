namespace GrimSpace.Math.Grid;

public readonly record struct GridBasis(Coord Forward, Coord Up, Coord Right)
{
	public static GridBasis From(Coord forward, Coord up, Coord right) =>
		new(forward, up, right);

	public Coord ToWorldOffset(int localForward, int localRight, int localUp) =>
		Forward * localForward + Right * localRight + Up * localUp;

	public Coord ToWorldCell(Coord origin, int localForward, int localRight, int localUp) =>
		origin + ToWorldOffset(localForward, localRight, localUp);

	public bool TryToLocal(Coord delta, out int localForward, out int localRight, out int localUp)
	{
		localForward = Coord.Dot(delta, Forward);
		localRight = Coord.Dot(delta, Right);
		localUp = Coord.Dot(delta, Up);
		return delta == ToWorldOffset(localForward, localRight, localUp);
	}
}
