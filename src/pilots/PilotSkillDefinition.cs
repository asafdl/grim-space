namespace GrimSpace.Pilots;

public sealed class PilotSkillDefinition
{
	public EPilotSkillId Id { get; }
	public IReadOnlyList<EPilotCapabilityId> CapabilityIds { get; }
	public string Name { get; }
	public string Description { get; }
	public int Tier { get; }
	public IReadOnlyList<EPilotSkillId> Prerequisites { get; }
	public int MinimumEarnedLevel { get; }

	public PilotSkillDefinition(
		EPilotSkillId id,
		IReadOnlyList<EPilotCapabilityId> capabilityIds,
		string name,
		string description,
		int tier,
		IReadOnlyList<EPilotSkillId>? prerequisites = null,
		int minimumEarnedLevel = 1)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(description);
		ArgumentNullException.ThrowIfNull(capabilityIds);
		if (capabilityIds.Count == 0)
			throw new ArgumentException("A pilot skill must grant at least one capability.", nameof(capabilityIds));
		ArgumentOutOfRangeException.ThrowIfLessThan(tier, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(minimumEarnedLevel, 1);

		Id = id;
		CapabilityIds = capabilityIds;
		Name = name;
		Description = description;
		Tier = tier;
		Prerequisites = prerequisites ?? Array.Empty<EPilotSkillId>();
		MinimumEarnedLevel = minimumEarnedLevel;
	}
}
