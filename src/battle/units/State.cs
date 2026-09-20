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
	public required ShipSpec Spec { get; init; }
	public Coord Position { get; set; }
	public Coord Fore { get; set; }
	public Coord Dorsal { get; set; }
	public Coord Starboard { get; set; }
	public int ActionPoints { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; } = new();
	public Dictionary<AbilityMount, MountRuntimeCounters> MountRuntime { get; } = new();
	public int FuelRemaining { get; set; }
	public string ParentId { get; set; } = BattleActorIds.Rules;
	public bool ApPenaltyNextTurn { get; set; }
	public required Stats Stats { get; init; }

	public bool IsAlive => HullPoints > 0;

	public InstalledAbility? FindInstalled(EAbilityKind kind, ESpatialOrientation mountedOn)
	{
		foreach (var installed in Spec.InstalledAbilities)
		{
			if (installed.Mount == new AbilityMount(kind, mountedOn))
				return installed;
		}

		return null;
	}

	public InstalledAbility? FindInstalled(EAbilityKind kind) =>
		Spec.InstalledAbilities.FirstOrDefault(installed => installed.Spec.Kind == kind);

	public MountRuntimeCounters MountRuntimeFor(AbilityMount mount) => MountRuntime[mount];

	public int UsesRemaining(AbilityMount mount) =>
		MountRuntime.TryGetValue(mount, out var runtime) ? runtime.UsesRemaining : 0;

	public int UsesRemaining(EAbilityKind kind) =>
		Spec.InstalledAbilities
			.Where(installed => installed.Spec.Kind == kind)
			.Sum(installed => UsesRemaining(installed.Mount));

	public int MaxUsesPerTurn(EAbilityKind kind) =>
		Spec.InstalledAbilities
			.Where(installed => installed.Spec.Kind == kind)
			.Sum(installed => installed.Spec is IPerTurnAbility perTurn ? perTurn.UsesPerTurn : 0);

	public int CooldownRemaining(AbilityMount mount) =>
		MountRuntime.TryGetValue(mount, out var runtime) ? runtime.CooldownRemaining : 0;

	public int ReadyMounts(EAbilityKind kind) =>
		Spec.InstalledAbilities.Count(installed =>
			installed.Spec.Kind == kind
			&& CooldownRemaining(installed.Mount) == 0);

	public int MountCount(EAbilityKind kind) =>
		Spec.InstalledAbilities.Count(installed => installed.Spec.Kind == kind);

	public State Clone()
	{
		var copy = new State
		{
			Id = Id,
			Type = Type,
			Spec = Spec.DeepCopy(),
			Position = Position,
			Fore = Fore,
			Dorsal = Dorsal,
			Starboard = Starboard,
			ActionPoints = ActionPoints,
			HullPoints = HullPoints,
			ShieldPoints = ShieldPoints.Clone(),
			FuelRemaining = FuelRemaining,
			ParentId = ParentId,
			ApPenaltyNextTurn = ApPenaltyNextTurn,
			Stats = Stats,
		};
		foreach (var (mount, runtime) in MountRuntime)
			copy.MountRuntime[mount] = runtime.Clone();
		return copy;
	}

	public static State FromShipInstance(ShipInstance ship, Coord position) =>
		FromShipInstance(ship, position, Coord.Forward, Coord.Up);

	public static State FromShipInstance(
		ShipInstance ship,
		Coord position,
		Coord fore,
		Coord dorsal,
		string parentId = BattleActorIds.Rules)
	{
		var stats = Stats.ForSpec(ship.Spec);
		var state = new State
		{
			Id = ship.Id,
			Type = ship.Spec.Chassis,
			Spec = ship.Spec.DeepCopy(),
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			ActionPoints = stats.MaxAp,
			HullPoints = ship.HullPoints,
			ShieldPoints = ship.ShieldPoints.Clone(),
			FuelRemaining = 0,
			ParentId = parentId,
			Stats = stats,
		};
		foreach (var installed in ship.Spec.InstalledAbilities)
			state.MountRuntime[installed.Mount] = installed.Spec.CreateInitialRuntime();
		return state;
	}
}
