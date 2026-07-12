using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed class Instance
{
	public required string Id { get; init; }
	public EType Type { get; init; }
	public EController Controller { get; init; }
}
