using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.Battle.Units;

public sealed class State
{
	public required string Id { get; init; }
	public Coord Position { get; set; }
	public Coord Fore { get; set; }
	public Coord Dorsal { get; set; }
	public Coord Starboard { get; set; }
	public int ActionPoints { get; set; }
	public int Hp { get; set; }
	public int MomentumLevel { get; set; }
	public int MissilesRemaining { get; set; }
	public bool ApPenaltyNextTurn { get; set; }
	public required Stats Stats { get; init; }

	public bool IsAlive => Hp > 0;

	public State Clone() =>
		new()
		{
			Id = Id,
			Position = Position,
			Fore = Fore,
			Dorsal = Dorsal,
			Starboard = Starboard,
			ActionPoints = ActionPoints,
			Hp = Hp,
			MomentumLevel = MomentumLevel,
			MissilesRemaining = MissilesRemaining,
			ApPenaltyNextTurn = ApPenaltyNextTurn,
			Stats = Stats,
		};

	public static State FromSpawn(Instance instance, Coord position)
	{
		var stats = Stats.ForType(instance.Type);
		var fore = Coord.Forward;
		var dorsal = Coord.Up;
		return new State
		{
			Id = instance.Id,
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			ActionPoints = stats.MaxAp,
			Hp = stats.MaxHp,
			MomentumLevel = 0,
			MissilesRemaining = stats.MissilesPerTurn,
			Stats = stats,
		};
	}
}
