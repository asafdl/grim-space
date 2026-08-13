using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

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

	public static FaceShieldPoints MaxFor(EType type)
	{
		var profile = new FaceShieldPoints();
		switch (type)
		{
			case EType.Fighter:
			case EType.Carrier:
				profile.Fill(2);
				break;
			case EType.Patrol:
				profile[ESpatialOrientation.Forward] = 3;
				break;
			case EType.Torpedo:
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}

		return profile;
	}

	public FaceShieldPoints Clone()
	{
		var copy = new FaceShieldPoints();
		Array.Copy(_points, copy._points, _points.Length);
		return copy;
	}

	public void Fill(int value) => Array.Fill(_points, value);
}
