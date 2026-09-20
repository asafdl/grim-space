using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Defenses;

public sealed class FaceShieldPoints
{
	private readonly int[] _points = new int[6];

	public int this[ESpatialOrientation face]
	{
		get => _points[(int)face];
		set => _points[(int)face] = value;
	}

	public int MaxOnAnyFace
	{
		get
		{
			var max = 0;
			foreach (var value in _points)
				max = System.Math.Max(max, value);
			return max;
		}
	}

	public FaceShieldPoints Clone()
	{
		var copy = new FaceShieldPoints();
		Array.Copy(_points, copy._points, _points.Length);
		return copy;
	}

	public void Fill(int value) => Array.Fill(_points, value);

	public bool Matches(FaceShieldPoints other)
	{
		for (var i = 0; i < _points.Length; i++)
		{
			if (_points[i] != other._points[i])
				return false;
		}

		return true;
	}
}
