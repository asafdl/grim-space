using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Battle.Player;

public sealed record UnitDisplayState(
	string Id,
	EType Type,
	ShipLoadout Loadout,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	int HullPoints,
	int MaxHullPoints,
	FaceShieldPoints ShieldPoints,
	int ActionPoints,
	int ManeuverPoints,
	ManeuverabilitySpec Maneuverability,
	IReadOnlyList<MountDisplayState> Mounts,
	int FuelRemaining,
	VoidBombProjectile? Projectile,
	bool IsAlive)
{
	public static UnitDisplayState Capture(State state) =>
		new(
			state.Id,
			state.Type,
			state.Loadout.DeepCopy(),
			state.Position,
			state.Fore,
			state.Dorsal,
			state.HullPoints,
			state.Loadout.MaxHullPoints,
			state.ShieldPoints.Clone(),
			state.ActionPoints,
			state.ManeuverPoints,
			state.Maneuverability,
			state.Loadout.InstalledAbilities
				.Select(installed => MountDisplayState.Capture(
					installed,
					state.MountRuntimeFor(installed.Mount)))
				.ToList(),
			state.FuelRemaining,
			state.Projectile,
			state.IsAlive);

	public int MaxActionPoints => Maneuverability.MaxActionPoints;
	public int MaxManeuverPoints => Maneuverability.MaxManeuverPoints;

	public int UsesRemaining(EAbilityKind kind) =>
		Mounts
			.Where(mount => mount.Mount.Kind == kind)
			.Sum(mount => mount.UsesRemaining);

	public int MaxUsesPerTurn(EAbilityKind kind) =>
		Mounts
			.Where(mount => mount.Mount.Kind == kind)
			.Sum(mount => mount.UsesPerTurn);

	public int ReadyMounts(EAbilityKind kind) =>
		Mounts.Count(mount =>
			mount.Mount.Kind == kind
			&& mount.CooldownRemaining == 0);

	public int MountCount(EAbilityKind kind) =>
		Mounts.Count(mount => mount.Mount.Kind == kind);

	public BodyFrame ToBodyFrame() =>
		new(Position, Fore, Dorsal, Coord.Cross(Dorsal, Fore));

	public State ToState()
	{
		var shields = ShieldPoints.Clone();
		var state = new State
		{
			Id = Id,
			Type = Type,
			Loadout = Loadout.DeepCopy(),
			Position = Position,
			Fore = Fore,
			Dorsal = Dorsal,
			Starboard = Coord.Cross(Dorsal, Fore),
			ActionPoints = ActionPoints,
			ManeuverPoints = ManeuverPoints,
			HullPoints = HullPoints,
			ShieldPoints = shields,
			FuelRemaining = FuelRemaining,
			Projectile = Projectile,
			Maneuverability = Maneuverability,
		};
		foreach (var mount in Mounts)
		{
			state.MountRuntime[mount.Mount] = new MountRuntimeCounters
			{
				UsesRemaining = mount.UsesRemaining,
				CooldownRemaining = mount.CooldownRemaining,
			};
		}

		return state;
	}
}
