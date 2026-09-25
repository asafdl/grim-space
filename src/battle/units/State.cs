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
	public required ShipLoadout Loadout { get; init; }
	public Coord Position { get; set; }
	public Coord Fore { get; set; }
	public Coord Dorsal { get; set; }
	public Coord Starboard { get; set; }
	public int ActionPoints { get; set; }
	public int HullPoints { get; set; }
	public FaceShieldPoints ShieldPoints { get; set; } = new();
	public Dictionary<AbilityMount, MountRuntimeCounters> MountRuntime { get; } = new();
	public int FuelRemaining { get; set; }
	public TorpedoProjectile? Projectile { get; set; }
	public string ParentId { get; set; } = BattleActorIds.Rules;
	public bool ApPenaltyNextTurn { get; set; }
	public required Stats Stats { get; set; }

	public bool IsAlive => HullPoints > 0;

	public TorpedoProjectile RequireProjectile() =>
		Projectile
		?? throw new InvalidOperationException($"Actor '{Id}' has no torpedo projectile profile.");

	public InstalledAbility? FindInstalled(EAbilityKind kind, ESpatialOrientation mountedOn)
	{
		foreach (var installed in Loadout.InstalledAbilities)
		{
			if (installed.Mount == new AbilityMount(kind, mountedOn))
				return installed;
		}

		return null;
	}

	public InstalledAbility? FindInstalled(EAbilityKind kind) =>
		Loadout.InstalledAbilities.FirstOrDefault(installed => installed.Spec.Kind == kind);

	public MountRuntimeCounters MountRuntimeFor(AbilityMount mount) => MountRuntime[mount];

	public int UsesRemaining(AbilityMount mount) =>
		MountRuntime.TryGetValue(mount, out var runtime) ? runtime.UsesRemaining : 0;

	public int UsesRemaining(EAbilityKind kind) =>
		Loadout.InstalledAbilities
			.Where(installed => installed.Spec.Kind == kind)
			.Sum(installed => UsesRemaining(installed.Mount));

	public int MaxUsesPerTurn(EAbilityKind kind) =>
		Loadout.InstalledAbilities
			.Where(installed => installed.Spec.Kind == kind)
			.Sum(installed => installed.Spec is IPerTurnAbility perTurn ? perTurn.UsesPerTurn : 0);

	public int CooldownRemaining(AbilityMount mount) =>
		MountRuntime.TryGetValue(mount, out var runtime) ? runtime.CooldownRemaining : 0;

	public int ReadyMounts(EAbilityKind kind) =>
		Loadout.InstalledAbilities.Count(installed =>
			installed.Spec.Kind == kind
			&& CooldownRemaining(installed.Mount) == 0);

	public int MountCount(EAbilityKind kind) =>
		Loadout.InstalledAbilities.Count(installed => installed.Spec.Kind == kind);

	public State Clone()
	{
		var copy = new State
		{
			Id = Id,
			Type = Type,
			Loadout = Loadout.DeepCopy(),
			Position = Position,
			Fore = Fore,
			Dorsal = Dorsal,
			Starboard = Starboard,
			ActionPoints = ActionPoints,
			HullPoints = HullPoints,
			ShieldPoints = ShieldPoints.Clone(),
			FuelRemaining = FuelRemaining,
			Projectile = Projectile,
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
		var projectile = ship.Spec.Chassis == EType.Torpedo
			? TorpedoProjectile.CatalogDefault()
			: null;
		var stats = ship.Spec.Chassis == EType.Torpedo && projectile is not null
			? new Stats { MaxAp = projectile.MovementActionPoints }
			: Stats.ForLoadout(ship.Spec, ship.Loadout);
		var state = new State
		{
			Id = ship.Id,
			Type = ship.Spec.Chassis,
			Loadout = ship.Loadout.DeepCopy(),
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			ActionPoints = stats.MaxAp,
			HullPoints = ship.HullPoints,
			ShieldPoints = ship.ShieldPoints.Clone(),
			FuelRemaining = 0,
			Projectile = projectile,
			ParentId = parentId,
			Stats = stats,
		};
		foreach (var installed in ship.Loadout.InstalledAbilities)
			state.MountRuntime[installed.Mount] = installed.Spec.CreateInitialRuntime();
		return state;
	}
}
