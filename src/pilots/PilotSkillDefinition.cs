namespace GrimSpace.Pilots;

public sealed class PilotSkillDefinition
{
	public EPilotSkillId Id { get; }
	public EPilotSkillTag Tag { get; }
	public string Name { get; }
	public string Description { get; }
	public int Tier { get; }
	public IReadOnlyList<EPilotSkillId> Prerequisites { get; }
	public int MinimumEarnedLevel { get; }

	public PilotSkillDefinition(
		EPilotSkillId id,
		EPilotSkillTag tag,
		string name,
		string description,
		int tier,
		IReadOnlyList<EPilotSkillId>? prerequisites = null,
		int minimumEarnedLevel = 1)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(description);
		ArgumentOutOfRangeException.ThrowIfLessThan(tier, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(minimumEarnedLevel, 1);

		Id = id;
		Tag = tag;
		Name = name;
		Description = description;
		Tier = tier;
		Prerequisites = prerequisites ?? Array.Empty<EPilotSkillId>();
		MinimumEarnedLevel = minimumEarnedLevel;
	}
}
