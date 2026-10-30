using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Units;

public sealed record ShipMountDelta(
	AbilityMount Mount,
	int DamageTiersDelta,
	int RangeTiersDelta);

public sealed class ShipHardwareDiff
{
	public EType BaselineChassis { get; }
	public EType CandidateChassis { get; }
	public EShipGearTier BaselineGearTier { get; }
	public EShipGearTier CandidateGearTier { get; }
	public int MaxHullDelta { get; }
	public IReadOnlyList<(ESpatialOrientation Face, int Delta)> ShieldDeltas { get; }
	public IReadOnlyList<InstalledAbility> AddedAbilities { get; }
	public IReadOnlyList<InstalledAbility> RemovedAbilities { get; }
	public IReadOnlyList<ShipMountDelta> ChangedMounts { get; }
	public int InitiativeDelta { get; }
	public int ActionPointsDelta { get; }
	public int ManeuverPointsDelta { get; }

	public bool ChassisChanged => BaselineChassis != CandidateChassis;
	public bool GearTierChanged => BaselineGearTier != CandidateGearTier;

	public bool IsEmpty =>
		!ChassisChanged
		&& !GearTierChanged
		&& MaxHullDelta == 0
		&& ShieldDeltas.Count == 0
		&& AddedAbilities.Count == 0
		&& RemovedAbilities.Count == 0
		&& ChangedMounts.Count == 0
		&& InitiativeDelta == 0
		&& ActionPointsDelta == 0
		&& ManeuverPointsDelta == 0;

	private ShipHardwareDiff(
		EType baselineChassis,
		EType candidateChassis,
		EShipGearTier baselineGearTier,
		EShipGearTier candidateGearTier,
		int maxHullDelta,
		IReadOnlyList<(ESpatialOrientation Face, int Delta)> shieldDeltas,
		IReadOnlyList<InstalledAbility> addedAbilities,
		IReadOnlyList<InstalledAbility> removedAbilities,
		IReadOnlyList<ShipMountDelta> changedMounts,
		int initiativeDelta,
		int actionPointsDelta,
		int maneuverPointsDelta)
	{
		BaselineChassis = baselineChassis;
		CandidateChassis = candidateChassis;
		BaselineGearTier = baselineGearTier;
		CandidateGearTier = candidateGearTier;
		MaxHullDelta = maxHullDelta;
		ShieldDeltas = shieldDeltas;
		AddedAbilities = addedAbilities;
		RemovedAbilities = removedAbilities;
		ChangedMounts = changedMounts;
		InitiativeDelta = initiativeDelta;
		ActionPointsDelta = actionPointsDelta;
		ManeuverPointsDelta = maneuverPointsDelta;
	}

	public static ShipHardwareDiff Compare(
		EType baselineChassis,
		ShipLoadout baselineLoadout,
		EType candidateChassis,
		ShipLoadout candidateLoadout)
	{
		ArgumentNullException.ThrowIfNull(baselineLoadout);
		ArgumentNullException.ThrowIfNull(candidateLoadout);

		var shieldDeltas = new List<(ESpatialOrientation Face, int Delta)>();
		foreach (ESpatialOrientation face in Enum.GetValues<ESpatialOrientation>())
		{
			var delta = candidateLoadout.MaxShieldPoints[face] - baselineLoadout.MaxShieldPoints[face];
			if (delta != 0)
				shieldDeltas.Add((face, delta));
		}

		var baselineByMount = baselineLoadout.InstalledAbilities.ToDictionary(installed => installed.Mount);
		var candidateByMount = candidateLoadout.InstalledAbilities.ToDictionary(installed => installed.Mount);

		var added = candidateLoadout.InstalledAbilities
			.Where(installed => !baselineByMount.ContainsKey(installed.Mount))
			.OrderBy(installed => installed.Kind)
			.ThenBy(installed => installed.MountedOn)
			.ToArray();

		var removed = baselineLoadout.InstalledAbilities
			.Where(installed => !candidateByMount.ContainsKey(installed.Mount))
			.OrderBy(installed => installed.Kind)
			.ThenBy(installed => installed.MountedOn)
			.ToArray();

		var changed = new List<ShipMountDelta>();
		foreach (var candidate in candidateLoadout.InstalledAbilities
			.OrderBy(installed => installed.Kind)
			.ThenBy(installed => installed.MountedOn))
		{
			if (!baselineByMount.TryGetValue(candidate.Mount, out var baseline))
				continue;

			var damageDelta = candidate.DamageUpgradeTier - baseline.DamageUpgradeTier;
			var rangeDelta = candidate.RangeUpgradeTier - baseline.RangeUpgradeTier;
			if (damageDelta == 0 && rangeDelta == 0)
				continue;

			changed.Add(new ShipMountDelta(candidate.Mount, damageDelta, rangeDelta));
		}

		var initiativeDelta = 0;
		var actionPointsDelta = 0;
		var maneuverPointsDelta = 0;
		if (baselineChassis != candidateChassis)
		{
			var baselineSpec = ShipCatalog.SpecFor(baselineChassis);
			var candidateSpec = ShipCatalog.SpecFor(candidateChassis);
			initiativeDelta = candidateSpec.Initiative - baselineSpec.Initiative;
			actionPointsDelta = candidateSpec.Maneuverability.MaxActionPoints
				- baselineSpec.Maneuverability.MaxActionPoints;
			maneuverPointsDelta = candidateSpec.Maneuverability.MaxManeuverPoints
				- baselineSpec.Maneuverability.MaxManeuverPoints;
		}

		return new ShipHardwareDiff(
			baselineChassis,
			candidateChassis,
			ShipLoadoutGearTier.FromLoadout(baselineLoadout, baselineChassis),
			ShipLoadoutGearTier.FromLoadout(candidateLoadout, candidateChassis),
			candidateLoadout.MaxHullPoints - baselineLoadout.MaxHullPoints,
			shieldDeltas,
			added,
			removed,
			changed,
			initiativeDelta,
			actionPointsDelta,
			maneuverPointsDelta);
	}
}
