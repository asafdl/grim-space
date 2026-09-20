using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

public interface IAreaDamage
{
	int Damage { get; }

	/// <param name="origin">Actor origin in world grid cells.</param>
	/// <param name="direction">Primary burst axis (e.g. port/starboard or forward step).</param>
	/// <param name="fore">Ship fore axis in world cell steps.</param>
	/// <param name="dorsal">Ship dorsal axis in world cell steps.</param>
	IReadOnlyList<Coord> GetArea(Coord origin, Coord direction, Coord fore, Coord dorsal);
}
