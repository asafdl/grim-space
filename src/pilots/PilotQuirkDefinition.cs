namespace GrimSpace.Pilots;

public sealed class PilotQuirkDefinition
{
	public EPilotQuirkId Id { get; }
	public EPilotQuirkPolarity Polarity { get; }
	public string Name { get; }
	public string Description { get; }
	public IReadOnlyList<EPilotCapabilityId> CapabilityIds { get; }

	public PilotQuirkDefinition(
		EPilotQuirkId id,
		EPilotQuirkPolarity polarity,
		IReadOnlyList<EPilotCapabilityId> capabilityIds,
		string name,
		string description)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(description);
		ArgumentNullException.ThrowIfNull(capabilityIds);

		Id = id;
		Polarity = polarity;
		CapabilityIds = capabilityIds;
		Name = name;
		Description = description;
	}
}
