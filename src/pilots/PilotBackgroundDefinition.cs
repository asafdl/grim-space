namespace GrimSpace.Pilots;

public sealed class PilotBackgroundDefinition
{
	public EPilotBackgroundId Id { get; }
	public IReadOnlyList<EPilotCapabilityId> CapabilityIds { get; }
	public string Name { get; }
	public string Description { get; }

	public PilotBackgroundDefinition(
		EPilotBackgroundId id,
		IReadOnlyList<EPilotCapabilityId> capabilityIds,
		string name,
		string description)
	{
		ArgumentNullException.ThrowIfNull(capabilityIds);
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(description);

		Id = id;
		CapabilityIds = capabilityIds;
		Name = name;
		Description = description;
	}
}
