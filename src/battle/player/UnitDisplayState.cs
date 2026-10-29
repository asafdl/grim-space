using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Battle.Player;

public sealed record UnitDisplayState(
	string Id,
	EType Type,
	int Initiative,
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
	HeatSpec HeatProfile,
	int Heat,
	bool Overdrive,
	bool Overheat,
	IReadOnlyList<MountDisplayState> Mounts,
	int FuelRemaining,
	VoidBombProjectile? Projectile,
	bool IsAlive,
	IReadOnlyList<PilotCapabilityGrant> PilotCapabilities,
	string? PilotPortraitId,
	bool ShieldsUpUsedThisTurn,
	int OverchargeEnginesCooldownRemaining,
	bool ShieldBraceActive = false,
	ESpatialOrientation? ShieldBraceFace = null,
	FaceShieldPoints? ShieldBraceSnapshot = null,
	int ShieldBraceDamagePending = 0,
	bool ShieldBraceUsedThisTurn = false)
{
	public static UnitDisplayState Capture(State state, ActorRuntime runtime) =>
		new(
			state.Id,
			state.Type,
			state.Initiative,
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
			state.HeatProfile,
			state.Heat,
			state.Overdrive,
			state.Overheat,
			state.Loadout.InstalledAbilities
				.Select(installed => MountDisplayState.Capture(
					installed,
					state.MountRuntimeFor(installed.Mount)))
				.ToList(),
			state.FuelRemaining,
			state.Projectile,
			state.IsAlive,
			runtime.PilotCapabilityGrants.ToArray(),
			runtime.PortraitId,
			state.ShieldsUpUsedThisTurn,
			state.OverchargeEnginesCooldownRemaining,
			state.ShieldBraceActive,
			state.ShieldBraceFace,
			state.ShieldBraceSnapshot?.Clone(),
			state.ShieldBraceDamagePending,
			state.ShieldBraceUsedThisTurn);

	public int MaxActionPoints => Maneuverability.MaxActionPoints;
	public int MaxManeuverPoints => Maneuverability.MaxManeuverPoints;
	public int HeatCapacity => HeatProfile.EffectiveCapacity;

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
			Initiative = Initiative,
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
			HeatProfile = HeatProfile,
			Heat = Heat,
			Overdrive = Overdrive,
			Overheat = Overheat,
			ShieldsUpUsedThisTurn = ShieldsUpUsedThisTurn,
			OverchargeEnginesCooldownRemaining = OverchargeEnginesCooldownRemaining,
			ShieldBraceActive = ShieldBraceActive,
			ShieldBraceFace = ShieldBraceFace,
			ShieldBraceSnapshot = ShieldBraceSnapshot?.Clone(),
			ShieldBraceDamagePending = ShieldBraceDamagePending,
			ShieldBraceUsedThisTurn = ShieldBraceUsedThisTurn,
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
