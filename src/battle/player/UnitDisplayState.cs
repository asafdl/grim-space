using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

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
	FaceShieldPoints MaxShieldPoints,
	int ActionPoints,
	int MaxActionPoints,
	int FlakRemaining,
	int FlaksPerTurn,
	int RailgunRemaining,
	int RailgunsPerTurn,
	int TorpedoCooldownRemaining,
	int PatrolSpawnCooldownRemaining,
	int FuelRemaining,
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
			state.Stats.MaxShieldPoints.Clone(),
			state.ActionPoints,
			state.Stats.MaxAp,
			state.FlakRemaining,
			state.Stats.FlaksPerTurn,
			state.RailgunRemaining,
			state.Stats.RailgunsPerTurn,
			state.TorpedoCooldownRemaining,
			state.PatrolSpawnCooldownRemaining,
			state.FuelRemaining,
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
			FlakRemaining = FlakRemaining,
			RailgunRemaining = RailgunRemaining,
			TorpedoCooldownRemaining = TorpedoCooldownRemaining,
			PatrolSpawnCooldownRemaining = PatrolSpawnCooldownRemaining,
			FuelRemaining = FuelRemaining,
			Stats = stats,
		};
	}
}
