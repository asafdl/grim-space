namespace GrimSpace.Pilots;

public enum EPilotCapabilityId
{
	ShieldBoost,
	AdjacentDamageBonus,
	LightningShieldDamageBonus,
	StationaryDamageDeflection,
	AlliedDamageReduction,
	ScrapRewardBonus,
	ShipUpgradeDiscount,
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
}
