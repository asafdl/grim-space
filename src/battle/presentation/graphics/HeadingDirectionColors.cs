using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class HeadingDirectionColors
{
	public static Color For(Coord heading) =>
		heading switch
		{
			{ X: 1, Y: 0, Z: 0 } => new Color(0.78f, 0.28f, 0.25f),
			{ X: -1, Y: 0, Z: 0 } => new Color(0.76f, 0.46f, 0.18f),
			{ X: 0, Y: 1, Z: 0 } => new Color(0.24f, 0.61f, 0.32f),
			{ X: 0, Y: -1, Z: 0 } => new Color(0.58f, 0.62f, 0.18f),
			{ X: 0, Y: 0, Z: 1 } => new Color(0.23f, 0.52f, 0.7f),
			{ X: 0, Y: 0, Z: -1 } => new Color(0.43f, 0.3f, 0.68f),
			_ => throw new ArgumentOutOfRangeException(nameof(heading), heading, null),
		};
}
