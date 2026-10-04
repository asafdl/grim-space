namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractBoardConfig
{
	public const int DefaultCadenceTicks = 5;
	public const int DefaultTtlTicks = 300;
	public const int DefaultMerchantRefreshCooldownTicks = 150;

	public int CadenceTicks { get; init; } = DefaultCadenceTicks;

	public int TtlTicks { get; init; } = DefaultTtlTicks;

	public ContractPlacementConfig Placement { get; init; } = new();

	public ContractNarrativePickerConfig Narrative { get; init; } = new();
}
