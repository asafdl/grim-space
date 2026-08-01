using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class State
{
	public required string Id { get; init; }
	public required EType Type { get; init; }
	public Coord Position { get; set; }
	public Coord Fore { get; set; }
	public Coord Dorsal { get; set; }
	public Coord Starboard { get; set; }
	public int ActionPoints { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; } = new();
	public int MomentumLevel { get; set; }
	public int FlakRemaining { get; set; }
	public int RailgunRemaining { get; set; }
	public bool ApPenaltyNextTurn { get; set; }
	public required Stats Stats { get; init; }

	public bool IsAlive => HullPoints > 0;

	public State Clone() =>
		new()
		{
			Id = Id,
			Type = Type,
			Position = Position,
			Fore = Fore,
			Dorsal = Dorsal,
			Starboard = Starboard,
			ActionPoints = ActionPoints,
			HullPoints = HullPoints,
			ShieldPoints = ShieldPoints.Clone(),
			MomentumLevel = MomentumLevel,
			FlakRemaining = FlakRemaining,
			RailgunRemaining = RailgunRemaining,
			ApPenaltyNextTurn = ApPenaltyNextTurn,
			Stats = Stats,
		};

	public static State FromSpawn(Instance instance, Coord position) =>
		FromSpawn(instance, position, Coord.Forward, Coord.Up);

	public static State FromSpawn(
		Instance instance,
		Coord position,
		Coord fore,
		Coord dorsal)
	{
		var stats = Stats.ForType(instance.Type);
		var shieldPoints = new FaceShieldPoints();
		shieldPoints.Fill(stats.MaxShieldPointsPerFace);
		return new State
		{
			Id = instance.Id,
			Type = instance.Type,
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			ActionPoints = stats.MaxAp,
			HullPoints = stats.MaxHullPoints,
			ShieldPoints = shieldPoints,
			MomentumLevel = 0,
			FlakRemaining = stats.FlaksPerTurn,
			RailgunRemaining = stats.RailgunsPerTurn,
			Stats = stats,
		};
	}
}
