using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle.Environment;

/// <summary>
/// Tracks active hazard zones and resolves their effects against units.
/// </summary>
public sealed class HazardSystem
{
	private readonly List<Hazard> _active = [];

	public IReadOnlyList<Hazard> Active => _active;

	/// <summary>Collection passed to action commit boards for hazard spawning.</summary>
	public ICollection<Hazard> RegisterTarget => _active;

	public HashSet<Coord> GetOccupiedCells()
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in _active)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public void ResolveAgainst(IReadOnlyList<Unit> units)
	{
		foreach (var unit in units)
		{
			if (!unit.State.IsAlive)
				continue;

			foreach (var hazard in _active)
			{
				if (!hazard.Cells.Contains(unit.State.Position))
					continue;

				ApplyDamage(unit.State, hazard.Damage);
				unit.State.MomentumLevel = System.Math.Max(
					unit.State.MomentumLevel - hazard.MomentumLoss,
					0);
			}
		}
	}

	public void Clear() => _active.Clear();

	private static void ApplyDamage(UnitState target, int damage) =>
		target.Hp = System.Math.Max(target.Hp - damage, 0);
}
