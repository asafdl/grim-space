using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Battle.Ids;
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
	public int FlakRemaining { get; set; }
	public int RailgunRemaining { get; set; }
	public Dictionary<AbilityMount, int> MountUsesRemaining { get; } = [];
	public int FuelRemaining { get; set; }
	public int TorpedoCooldownRemaining { get; set; }
	public int PatrolSpawnCooldownRemaining { get; set; }
	public string ParentId { get; set; } = BattleActorIds.Rules;
	public bool ApPenaltyNextTurn { get; set; }
	public required Stats Stats { get; init; }

	public bool IsAlive => HullPoints > 0;

	public State Clone()
	{
		var copy = new State
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
			FlakRemaining = FlakRemaining,
			RailgunRemaining = RailgunRemaining,
			FuelRemaining = FuelRemaining,
			TorpedoCooldownRemaining = TorpedoCooldownRemaining,
			PatrolSpawnCooldownRemaining = PatrolSpawnCooldownRemaining,
			ParentId = ParentId,
			ApPenaltyNextTurn = ApPenaltyNextTurn,
			Stats = Stats,
		};
		foreach (var (mount, uses) in MountUsesRemaining)
			copy.MountUsesRemaining[mount] = uses;
		return copy;
	}

	public static State FromSpawn(Instance instance, Coord position) =>
		FromSpawn(instance, position, Coord.Forward, Coord.Up);

	public static State FromSpawn(
		Instance instance,
		Coord position,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var stats = Stats.ForType(instance.Type);
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
			ShieldPoints = stats.MaxShieldPoints.Clone(),
			FlakRemaining = stats.FlaksPerTurn,
			RailgunRemaining = stats.RailgunsPerTurn,
			FuelRemaining = 0,
			TorpedoCooldownRemaining = 0,
			PatrolSpawnCooldownRemaining = 0,
			ParentId = parentId,
			Stats = stats,
		};
	}

	public static State FromSnapshot(
		ShipSnapshot snapshot,
		Coord position,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var stats = Stats.ForType(snapshot.Configuration.Chassis);
		var state = new State
		{
			Id = snapshot.Id,
			Type = snapshot.Configuration.Chassis,
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			ActionPoints = stats.MaxAp,
			HullPoints = snapshot.HullPoints,
			ShieldPoints = snapshot.ShieldPoints.Clone(),
			FlakRemaining = AbilityLoadout.UsesPerTurnForAbility(snapshot, EAbilityKind.Flak),
			RailgunRemaining = AbilityLoadout.UsesPerTurnForAbility(snapshot, EAbilityKind.Railgun),
			FuelRemaining = 0,
			TorpedoCooldownRemaining = 0,
			PatrolSpawnCooldownRemaining = 0,
			ParentId = parentId,
			Stats = stats,
		};
		foreach (var (mount, uses) in AbilityLoadout.UsesPerMount(snapshot))
			state.MountUsesRemaining[mount] = uses;
		return state;
	}
}
