using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Player;

public sealed record UnitDisplayState(
	string Id,
	EType Type,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	int HullPoints,
	int MaxHullPoints,
	FaceShieldPoints ShieldPoints,
	int MaxShieldPointsPerFace,
	int MomentumLevel,
	int ActionPoints,
	int MaxActionPoints,
	int FlakRemaining,
	int FlaksPerTurn,
	int RailgunRemaining,
	int RailgunsPerTurn,
	int TorpedoCooldownRemaining,
	bool IsAlive)
{
	public static UnitDisplayState Capture(State state) =>
		new(
			state.Id,
			state.Type,
			state.Position,
			state.Fore,
			state.Dorsal,
			state.HullPoints,
			state.Stats.MaxHullPoints,
			state.ShieldPoints.Clone(),
			state.Stats.MaxShieldPointsPerFace,
			state.MomentumLevel,
			state.ActionPoints,
			state.Stats.MaxAp,
			state.FlakRemaining,
			state.Stats.FlaksPerTurn,
			state.RailgunRemaining,
			state.Stats.RailgunsPerTurn,
			state.TorpedoCooldownRemaining,
			state.IsAlive);

	public BodyFrame ToBodyFrame() =>
		new(Position, Fore, Dorsal, Coord.Cross(Dorsal, Fore));

	public State ToState()
	{
		var stats = Stats.ForType(Type);
		var shields = ShieldPoints.Clone();
		return new State
		{
			Id = Id,
			Type = Type,
			Position = Position,
			Fore = Fore,
			Dorsal = Dorsal,
			Starboard = Coord.Cross(Dorsal, Fore),
			ActionPoints = ActionPoints,
			HullPoints = HullPoints,
			ShieldPoints = shields,
			MomentumLevel = MomentumLevel,
			FlakRemaining = FlakRemaining,
			RailgunRemaining = RailgunRemaining,
			TorpedoCooldownRemaining = TorpedoCooldownRemaining,
			Stats = stats,
		};
	}
}
