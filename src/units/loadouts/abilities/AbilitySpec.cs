using GrimSpace.Math.Grid;
using GrimSpace.Units.Specs;

namespace GrimSpace.Units.Loadouts.Abilities;

public abstract record AbilitySpec
{
	public abstract EAbilityKind Kind { get; }

	public virtual int DamageUpgradeTier { get; init; }
	public virtual int RangeUpgradeTier { get; init; }
	public virtual int MaxDamageUpgrades => 0;
	public virtual int MaxRangeUpgrades => 0;

	public static AbilitySpec BaselineFor(EAbilityKind kind) =>
		kind switch
		{
			EAbilityKind.ScrapDroneSwarm => ScrapDroneSwarmSpec.Baseline,
			EAbilityKind.LightningCannon => LightningCannonSpec.Baseline,
			EAbilityKind.MinerBay => MinerBaySpec.Baseline,
			EAbilityKind.VoidBombLauncher => VoidBombLauncherSpec.Baseline,
			EAbilityKind.GoopGun => GoopGunSpec.Baseline,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};

	public static AbilitySpec Create(
		EAbilityKind kind,
		int damageUpgradeTier = 0,
		int rangeUpgradeTier = 0)
	{
		var baseline = BaselineFor(kind);
		if (damageUpgradeTier < 0 || damageUpgradeTier > baseline.MaxDamageUpgrades)
			throw new ArgumentOutOfRangeException(
				nameof(damageUpgradeTier),
				$"{kind} damage upgrade tier must be between 0 and {baseline.MaxDamageUpgrades}.");
		if (rangeUpgradeTier < 0 || rangeUpgradeTier > baseline.MaxRangeUpgrades)
			throw new ArgumentOutOfRangeException(
				nameof(rangeUpgradeTier),
				$"{kind} range upgrade tier must be between 0 and {baseline.MaxRangeUpgrades}.");

		return baseline.ApplyUpgrades(damageUpgradeTier, rangeUpgradeTier);
	}

	protected virtual AbilitySpec ApplyUpgrades(int damageUpgradeTier, int rangeUpgradeTier) => this;

	public MountRuntimeCounters CreateInitialRuntime() =>
		this switch
		{
			IPerTurnAbility perTurn => new MountRuntimeCounters
			{
				UsesRemaining = perTurn.UsesPerTurn,
				CooldownRemaining = 0,
			},
			ICooldownAbility => new MountRuntimeCounters
			{
				UsesRemaining = 0,
				CooldownRemaining = 0,
			},
			_ => throw new InvalidOperationException($"Unknown ability spec '{Kind}'."),
		};

	public void AdvanceRound(MountRuntimeCounters runtime)
	{
		switch (this)
		{
			case IPerTurnAbility perTurn:
				runtime.UsesRemaining = perTurn.UsesPerTurn;
				break;
			case ICooldownAbility:
				if (runtime.CooldownRemaining > 0)
					runtime.CooldownRemaining--;
				break;
			default:
				throw new InvalidOperationException($"Unknown ability spec '{Kind}'.");
		}
	}
}

public sealed record ScrapDroneSwarmSpec(
	int UsesPerTurn,
	int Damage,
	int Heal,
	int BurstRange) : AbilitySpec, IAreaDamage, IPerTurnAbility
{
	public const int MaxDamageUpgradeTier = 3;
	public const int MaxRangeUpgradeTier = 3;
	public static ScrapDroneSwarmSpec Baseline { get; } = new(
		UsesPerTurn: 1,
		Damage: 100,
		Heal: 100,
		BurstRange: 2);

	public override EAbilityKind Kind => EAbilityKind.ScrapDroneSwarm;
	public override int MaxDamageUpgrades => MaxDamageUpgradeTier;
	public override int MaxRangeUpgrades => MaxRangeUpgradeTier;
	int IPerTurnAbility.UsesPerTurn => UsesPerTurn;
	int IAreaDamage.Damage => Damage;

	protected override AbilitySpec ApplyUpgrades(int damageUpgradeTier, int rangeUpgradeTier) =>
		this with
		{
			Damage = Baseline.Damage + damageUpgradeTier * 100,
			BurstRange = BurstRange + rangeUpgradeTier,
			DamageUpgradeTier = damageUpgradeTier,
			RangeUpgradeTier = rangeUpgradeTier,
		};

	public IReadOnlyList<Coord> GetArea(Coord origin, Coord direction, Coord fore, Coord dorsal)
	{
		var starboard = Coord.Cross(dorsal, fore);
		var (apexPort, outwardStep) = Coord.Dot(direction, starboard) > 0 ? (-1, -1) : (1, 1);
		var cells = new List<Coord>();
		for (var outward = 0; outward <= BurstRange; outward++)
		{
			var port = apexPort + outwardStep * outward;
			for (var foreOffset = -outward; foreOffset <= outward; foreOffset++)
			{
				for (var dorsalOffset = -outward; dorsalOffset <= outward; dorsalOffset++)
				{
					if (System.Math.Abs(foreOffset) + System.Math.Abs(dorsalOffset) > outward)
						continue;

					cells.Add(origin + fore * foreOffset + starboard * (-port) + dorsal * dorsalOffset);
				}
			}
		}

		return cells;
	}
}

public sealed record LightningCannonSpec(
	int UsesPerTurn,
	int Damage,
	int LineLength,
	int MaxChains,
	int ChainRange = 3,
	int ChainFalloffPercentPerHop = 25) : AbilitySpec, IAreaDamage, IPerTurnAbility
{
	public const int MaxDamageUpgradeTier = 3;
	public const int MaxRangeUpgradeTier = 3;
	public static LightningCannonSpec Baseline { get; } = new(
		UsesPerTurn: 1,
		Damage: 200,
		LineLength: 7,
		MaxChains: 1);

	public override EAbilityKind Kind => EAbilityKind.LightningCannon;
	public override int MaxDamageUpgrades => MaxDamageUpgradeTier;
	public override int MaxRangeUpgrades => MaxRangeUpgradeTier;
	int IPerTurnAbility.UsesPerTurn => UsesPerTurn;
	int IAreaDamage.Damage => Damage;

	protected override AbilitySpec ApplyUpgrades(int damageUpgradeTier, int rangeUpgradeTier) =>
		this with
		{
			Damage = Baseline.Damage + damageUpgradeTier * 100,
			LineLength = Baseline.LineLength + rangeUpgradeTier,
			DamageUpgradeTier = damageUpgradeTier,
			RangeUpgradeTier = rangeUpgradeTier,
		};

	public IReadOnlyList<Coord> GetArea(Coord origin, Coord direction, Coord fore, Coord dorsal)
	{
		var cells = new List<Coord>(LineLength);
		for (var along = 1; along <= LineLength; along++)
			cells.Add(origin + direction * along);

		return cells;
	}
}

public sealed record MinerBaySpec(
	int CooldownTurns,
	ShipSpec ChildSpec,
	int MaxLivingChildren) : AbilitySpec, ISpawnable, ICooldownAbility
{
	public static MinerBaySpec Baseline { get; } = new(
		CooldownTurns: 2,
		RepurposedMinerSpec.Instance,
		MaxLivingChildren: 5);

	public override EAbilityKind Kind => EAbilityKind.MinerBay;
	int ICooldownAbility.CooldownTurns => CooldownTurns;
	ShipSpec ISpawnable.ChildSpec => ChildSpec;
	int ISpawnable.MaxLivingChildren => MaxLivingChildren;
}

public sealed record VoidBombLauncherSpec(
	int CooldownTurns,
	int FuelTurns,
	int MovementActionPoints,
	int ForwardMoveApCost,
	int LateralMoveApCost,
	int BlastRadius,
	int BlastDamage) : AbilitySpec, ISpawnable, ICooldownAbility
{
	public static VoidBombLauncherSpec Baseline { get; } = new(
		CooldownTurns: 3,
		FuelTurns: VoidBombSpec.FuelTurns,
		MovementActionPoints: VoidBombSpec.MovementActionPoints,
		ForwardMoveApCost: VoidBombSpec.ForwardMoveApCost,
		LateralMoveApCost: VoidBombSpec.LateralMoveApCost,
		BlastRadius: VoidBombSpec.BlastRadius,
		BlastDamage: VoidBombSpec.BlastDamage);

	public override EAbilityKind Kind => EAbilityKind.VoidBombLauncher;
	public override int MaxDamageUpgrades => 1;
	public override int MaxRangeUpgrades => 1;
	int ICooldownAbility.CooldownTurns => CooldownTurns;
	ShipSpec ISpawnable.ChildSpec => VoidBombSpec.Instance;
	int ISpawnable.MaxLivingChildren => 0;

	protected override AbilitySpec ApplyUpgrades(int damageUpgradeTier, int rangeUpgradeTier) =>
		this with
		{
			FuelTurns = FuelTurns + rangeUpgradeTier,
			BlastRadius = BlastRadius + rangeUpgradeTier,
			BlastDamage = Baseline.BlastDamage + damageUpgradeTier * 200,
			DamageUpgradeTier = damageUpgradeTier,
			RangeUpgradeTier = rangeUpgradeTier,
		};

	public int? MoveApCost(ESpatialOrientation direction) =>
		direction switch
		{
			ESpatialOrientation.Forward => ForwardMoveApCost,
			ESpatialOrientation.Port
				or ESpatialOrientation.Starboard
				or ESpatialOrientation.Dorsal
				or ESpatialOrientation.Ventral => LateralMoveApCost,
			_ => null,
		};
}

public sealed record GoopGunSpec(
	int Range = 3,
	int HalfWidth = 1,
	int HalfHeight = 1,
	int UnavailableTurns = 2) : AbilitySpec, ICooldownAbility, IAreaDamage
{
	public static GoopGunSpec Baseline { get; } = new();

	int IAreaDamage.Damage => 0;

	public override EAbilityKind Kind => EAbilityKind.GoopGun;
	int ICooldownAbility.CooldownTurns => UnavailableTurns;

	public IReadOnlyList<Coord> GetArea(Coord origin, Coord direction, Coord fore, Coord dorsal)
	{
		var starboard = Coord.Cross(dorsal, fore);
		var center = origin + direction * Range;
		var cells = new List<Coord>();
		for (var port = -HalfWidth; port <= HalfWidth; port++)
		{
			for (var dorsalOffset = -HalfHeight; dorsalOffset <= HalfHeight; dorsalOffset++)
				cells.Add(center + starboard * (-port) + dorsal * dorsalOffset);
		}

		return cells;
	}
}

public interface IPerTurnAbility
{
	int UsesPerTurn { get; }
}

public interface ICooldownAbility
{
	int CooldownTurns { get; }
}
