using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.Units.Loadouts.Abilities;

public abstract record AbilitySpec
{
	public abstract EAbilityKind Kind { get; }
	public abstract IReadOnlyList<ESpatialOrientation> CompatibleFacets { get; }

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

public sealed record FlakSpec(int UsesPerTurn, int Damage, int BurstRange, int UpgradeTier = 0)
	: AbilitySpec, IAreaDamage, IPerTurnAbility
{
	private static readonly ESpatialOrientation[] DefaultFacets =
		[ESpatialOrientation.Port, ESpatialOrientation.Starboard];

	public override EAbilityKind Kind => EAbilityKind.Flak;
	public override IReadOnlyList<ESpatialOrientation> CompatibleFacets => DefaultFacets;
	int IPerTurnAbility.UsesPerTurn => UsesPerTurn;
	int IAreaDamage.Damage => Damage;

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

public sealed record RailgunSpec(int UsesPerTurn, int Damage, int LineLength, int PyramidRange, int UpgradeTier = 0)
	: AbilitySpec, IAreaDamage, IPerTurnAbility
{
	private static readonly ESpatialOrientation[] DefaultFacets = [ESpatialOrientation.Forward];

	public override EAbilityKind Kind => EAbilityKind.Railgun;
	public override IReadOnlyList<ESpatialOrientation> CompatibleFacets => DefaultFacets;
	int IPerTurnAbility.UsesPerTurn => UsesPerTurn;
	int IAreaDamage.Damage => Damage;

	public IReadOnlyList<Coord> GetArea(Coord origin, Coord direction, Coord fore, Coord dorsal)
	{
		var starboard = Coord.Cross(dorsal, fore);
		var cells = new List<Coord>();
		for (var foreOffset = 1; foreOffset <= LineLength; foreOffset++)
			cells.Add(origin + direction * foreOffset);

		for (var depth = 0; depth <= PyramidRange; depth++)
		{
			var foreOffset = LineLength + depth;
			for (var port = -depth; port <= depth; port++)
			{
				for (var dorsalOffset = -depth; dorsalOffset <= depth; dorsalOffset++)
				{
					if (System.Math.Abs(port) + System.Math.Abs(dorsalOffset) > depth)
						continue;

					cells.Add(
						origin
						+ direction * foreOffset
						+ starboard * (-port)
						+ dorsal * dorsalOffset);
				}
			}
		}

		return cells;
	}
}

public sealed record PatrolBaySpec(
	int CooldownTurns,
	ShipSpec ChildSpec,
	int MaxLivingChildren,
	int UpgradeTier = 0) : AbilitySpec, ISpawnable, ICooldownAbility
{
	private static readonly ESpatialOrientation[] DefaultFacets = [ESpatialOrientation.Ventral];

	public override EAbilityKind Kind => EAbilityKind.PatrolBay;
	public override IReadOnlyList<ESpatialOrientation> CompatibleFacets => DefaultFacets;
	int ICooldownAbility.CooldownTurns => CooldownTurns;
	ShipSpec ISpawnable.ChildSpec => ChildSpec;
	int ISpawnable.MaxLivingChildren => MaxLivingChildren;
}

public sealed record TorpedoLauncherSpec(int CooldownTurns, ShipSpec ChildSpec, int UpgradeTier = 0)
	: AbilitySpec, ISpawnable, ICooldownAbility
{
	private static readonly ESpatialOrientation[] DefaultFacets =
	[
		ESpatialOrientation.Retro,
		ESpatialOrientation.Ventral,
		ESpatialOrientation.Dorsal,
	];

	public override EAbilityKind Kind => EAbilityKind.TorpedoLauncher;
	public override IReadOnlyList<ESpatialOrientation> CompatibleFacets => DefaultFacets;
	int ICooldownAbility.CooldownTurns => CooldownTurns;
	ShipSpec ISpawnable.ChildSpec => ChildSpec;
	int ISpawnable.MaxLivingChildren => 0;
}

public interface IPerTurnAbility
{
	int UsesPerTurn { get; }
}

public interface ICooldownAbility
{
	int CooldownTurns { get; }
}
