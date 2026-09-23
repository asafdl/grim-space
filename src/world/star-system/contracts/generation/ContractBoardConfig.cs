namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractBoardConfig
{
	public const int DefaultCadenceTicks = 5;
	public const int DefaultTtlTicks = 1000;

	public int CadenceTicks { get; init; } = DefaultCadenceTicks;

	public int TtlTicks { get; init; } = DefaultTtlTicks;

	public ContractPlacementConfig Placement { get; init; } = new();

	public ContractNarrativePickerConfig Narrative { get; init; } = new();
}
