using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed class Instance
{
	public string Id { get; init; } = "";

	public EType Type { get; init; }
	public required Alliance Alliance { get; init; }
}
