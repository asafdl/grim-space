// Placeholder until roguelike sector map exists.

using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

public sealed class Encounter
{
	public required IReadOnlyList<Spawn> Spawns { get; init; }

	public static Encounter DevDefault()
	{
		var player = new Instance
		{
			Id = "player",
			Type = EType.Fighter,
			Controller = EController.Player,
		};
		var enemy = new Instance
		{
			Id = "enemy",
			Type = EType.Fighter,
			Controller = EController.Enemy,
		};

		return new Encounter
		{
			Spawns =
			[
				new Spawn { Unit = player, Position = new Coord(30, 32, 32), InitialMomentum = 0 },
				new Spawn { Unit = enemy, Position = new Coord(36, 32, 32), InitialMomentum = 2 },
			],
		};
	}
}
