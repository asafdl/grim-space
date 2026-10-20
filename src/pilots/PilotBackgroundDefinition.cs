namespace GrimSpace.Pilots;

public sealed class PilotBackgroundDefinition
{
	public EPilotBackgroundId Id { get; }
	public string Name { get; }
	public string Description { get; }

	public PilotBackgroundDefinition(
		EPilotBackgroundId id,
		string name,
		string description)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentException.ThrowIfNullOrEmpty(description);

		Id = id;
		Name = name;
		Description = description;
	}
}
