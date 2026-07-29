using GrimSpace.Battle.Movement.Enums;

namespace GrimSpace.Battle.Units;

public sealed class FaceShieldPoints
{
	private readonly int[] _points = new int[6];

	public int this[ESpatialOrientation face]
	{
		get => _points[(int)face];
		set => _points[(int)face] = value;
	}

	public FaceShieldPoints Clone()
	{
		var copy = new FaceShieldPoints();
		Array.Copy(_points, copy._points, _points.Length);
		return copy;
	}

	public void Fill(int value) => Array.Fill(_points, value);
}
