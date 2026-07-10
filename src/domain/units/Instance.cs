using GrimSpace.Domain.Units.Enums;

namespace GrimSpace.Domain.Units;

public sealed class Instance
{
	public required string Id { get; init; }
	public EType Type { get; init; }
	public EController Controller { get; init; }
}
