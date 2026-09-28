using GrimSpace.Battle.Encounter;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter.Generation;

public static class AsteroidFieldReservations
{
	private const int ForwardAhead = 8;
	private const int AftForCamera = 14;
	private const int Lateral = 4;

	public static HashSet<Coord> PlayerCorridors(IEnumerable<BattleSpawn> spawns)
	{
		var reserved = new HashSet<Coord>();
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Player)
				continue;

			reserved.UnionWith(PlayerCorridor(spawn));
		}

		return reserved;
	}

	public static HashSet<Coord> PlayerCorridor(BattleSpawn spawn)
	{
		var basis = GridBasis.From(
			spawn.Fore,
			spawn.Dorsal,
			Coord.Cross(spawn.Dorsal, spawn.Fore));
		var cells = new HashSet<Coord>();
		for (var fore = -AftForCamera; fore <= ForwardAhead; fore++)
		for (var port = -Lateral; port <= Lateral; port++)
		for (var dorsal = -Lateral; dorsal <= Lateral; dorsal++)
			cells.Add(basis.ToWorldCell(spawn.Position, fore, port, dorsal));

		return cells;
	}
}
