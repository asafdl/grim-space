namespace GrimSpace.Pilots;

public enum EPilotCapabilityId
{
	ShieldBoost,
	OverchargeEngines,
	AdjacentDamageBonus,
	LightningShieldDamageBonus,
	ScrapDroneSwarmDamageBonus,
	ShieldBrace,
	StationaryDamageDeflection,
	InTheFray,
	ReinforcedHull,
	ExperienceGainBonus,
	HealingSkillRangeBonus,
	OneWithTheVoid,
	MinorScrapRewardBonus,
	ScrapRewardBonus,
	ShipUpgradeDiscount,
	OptimalityContractCreditBonus,
	InitiativePenalty,
}

public enum EPilotCapabilitySourceKind
{
	Skill,
	Quirk,
	Background,
}

public readonly record struct PilotCapabilitySource(
	EPilotCapabilitySourceKind Kind,
	string Id);

public readonly record struct PilotCapabilityGrant(
	EPilotCapabilityId CapabilityId,
	string PilotId,
	PilotCapabilitySource Source)
{
	public static IReadOnlyList<PilotCapabilityGrant> FromBackground(
		string pilotId,
		EPilotBackgroundId backgroundId,
		Func<EPilotCapabilityId, bool>? capabilityMatches = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		return Project(
			pilotId,
			PilotBackgroundCatalog.Get(backgroundId).CapabilityIds,
			EPilotCapabilitySourceKind.Background,
			backgroundId.ToString(),
			capabilityMatches);
	}

	public static IReadOnlyList<PilotCapabilityGrant> FromSkills(
		string pilotId,
		IEnumerable<EPilotSkillId> skillIds,
		Func<EPilotCapabilityId, bool>? capabilityMatches = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentNullException.ThrowIfNull(skillIds);
		var grants = new List<PilotCapabilityGrant>();
		foreach (var skillId in skillIds)
		{
			foreach (var capabilityId in PilotSkillCatalog.Get(skillId).CapabilityIds)
			{
				if (capabilityMatches is not null && !capabilityMatches(capabilityId))
					continue;

				grants.Add(new PilotCapabilityGrant(
					capabilityId,
					pilotId,
					new PilotCapabilitySource(
						EPilotCapabilitySourceKind.Skill,
						skillId.ToString())));
			}
		}

		return grants;
	}

	public static IReadOnlyList<PilotCapabilityGrant> FromQuirks(
		string pilotId,
		IEnumerable<EPilotQuirkId> quirkIds,
		Func<EPilotCapabilityId, bool>? capabilityMatches = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		ArgumentNullException.ThrowIfNull(quirkIds);
		var grants = new List<PilotCapabilityGrant>();
		foreach (var quirkId in quirkIds)
		{
			foreach (var capabilityId in PilotQuirkCatalog.Get(quirkId).CapabilityIds)
			{
				if (capabilityMatches is not null && !capabilityMatches(capabilityId))
					continue;

				grants.Add(new PilotCapabilityGrant(
					capabilityId,
					pilotId,
					new PilotCapabilitySource(
						EPilotCapabilitySourceKind.Quirk,
						quirkId.ToString())));
			}
		}

		return grants;
	}

	private static IReadOnlyList<PilotCapabilityGrant> Project(
		string pilotId,
		IEnumerable<EPilotCapabilityId> capabilityIds,
		EPilotCapabilitySourceKind sourceKind,
		string sourceId,
		Func<EPilotCapabilityId, bool>? capabilityMatches)
	{
		var grants = new List<PilotCapabilityGrant>();
		foreach (var capabilityId in capabilityIds)
		{
			if (capabilityMatches is not null && !capabilityMatches(capabilityId))
				continue;

			grants.Add(new PilotCapabilityGrant(
				capabilityId,
				pilotId,
				new PilotCapabilitySource(sourceKind, sourceId)));
		}

		return grants;
	}
}
