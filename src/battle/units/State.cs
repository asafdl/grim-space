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
	public FaceShieldPoints MaxShieldPoints { get; set; } = new();
	public FaceShieldPoints ShieldPoints { get; set; } = new();
	public Dictionary<EAbilityKind, MountRuntimeCounters> MountRuntime { get; } = new();
	public int FuelRemaining { get; set; }
	public string ParentId { get; set; } = BattleActorIds.Rules;
	public bool ApPenaltyNextTurn { get; set; }
	public required Stats Stats { get; init; }

	public bool IsAlive => HullPoints > 0;

	public InstalledAbility? FindInstalled(EAbilityKind kind, ESpatialOrientation? facet = null)
	{
		foreach (var installed in Spec.InstalledAbilities)
		{
			if (installed.Spec.Kind != kind)
				continue;

			if (facet is { } requiredFacet && !installed.Facets.Contains(requiredFacet))
				continue;

			return installed;
		}

		return null;
	}

	public MountRuntimeCounters MountRuntimeFor(EAbilityKind kind) => MountRuntime[kind];

	public int UsesRemaining(EAbilityKind kind)
	{
		var installed = FindInstalled(kind);
		if (installed is null || installed.Spec is not IPerTurnAbility)
			return 0;

		return MountRuntime[kind].UsesRemaining;
	}

	public int MaxUsesPerTurn(EAbilityKind kind)
	{
		var installed = FindInstalled(kind);
		if (installed is null || installed.Spec is not IPerTurnAbility perTurn)
			return 0;

		return perTurn.UsesPerTurn;
	}

	public int CooldownRemaining(EAbilityKind kind)
	{
		var installed = FindInstalled(kind);
		if (installed is null || installed.Spec is not ICooldownAbility)
			return 0;

		return MountRuntime[kind].CooldownRemaining;
	}

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
			MaxShieldPoints = MaxShieldPoints.Clone(),
			ShieldPoints = ShieldPoints.Clone(),
			FuelRemaining = FuelRemaining,
			ParentId = ParentId,
			ApPenaltyNextTurn = ApPenaltyNextTurn,
			Stats = Stats,
		};
		foreach (var (kind, runtime) in MountRuntime)
			copy.MountRuntime[kind] = runtime.Clone();
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
			MaxShieldPoints = ship.MaxShieldPoints.Clone(),
			ShieldPoints = ship.ShieldPoints.Clone(),
			FuelRemaining = 0,
			ParentId = parentId,
			Stats = stats,
		};
		foreach (var installed in ship.Spec.InstalledAbilities)
			state.MountRuntime[installed.Spec.Kind] = installed.ForState();
		return state;
	}
}
