using GrimSpace.Battle.Board;
using GrimSpace.Battle.Ids;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Environment;

/// <summary>
/// Board hazards are persistent terrain; turn hazards are temporary zones resolved each environment phase.
/// </summary>
public sealed class HazardSystem
{
	private readonly Dictionary<string, NonUnit> _nonUnits = new();

	public IReadOnlyDictionary<string, NonUnit> NonUnits => _nonUnits;

	public IReadOnlyList<Hazard> Board => _nonUnits.Values
		.OfType<Hazard>()
		.Where(hazard => hazard.OwnerId == EntityIds.Board)
		.ToList();

	public IReadOnlyList<Hazard> Active => _nonUnits.Values
		.OfType<Hazard>()
		.Where(hazard => hazard.OwnerId != EntityIds.Board)
		.ToList();

	public IDictionary<string, NonUnit> MutableNonUnits => _nonUnits;

	public void RegisterBoard(IEnumerable<Hazard> hazards)
	{
		foreach (var hazard in hazards)
			_nonUnits[hazard.Id] = hazard;
	}

	public HashSet<Coord> GetBlockedCells()
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in Board)
		{
			if (!hazard.Passable)
				cells.UnionWith(hazard.Cells);
		}

		return cells;
	}

	public HashSet<Coord> GetOccupiedCells()
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in Active)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public void Clear()
	{
		var turnScoped = _nonUnits.Values
			.Where(nonUnit => nonUnit.OwnerId != EntityIds.Board)
			.Select(nonUnit => nonUnit.Id)
			.ToList();

		foreach (var id in turnScoped)
			_nonUnits.Remove(id);
	}
}
