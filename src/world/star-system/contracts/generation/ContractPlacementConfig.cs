namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractPlacementConfig
{
	public const float DefaultHuntKindWeight = 1.5f;
	public const float DefaultDeliveryKindWeight = 1.0f;

	public int TargetGeneratedCount { get; init; } = 3;

	public float HuntKindWeight { get; init; } = DefaultHuntKindWeight;

	public float DeliveryKindWeight { get; init; } = DefaultDeliveryKindWeight;

	public int MaxPendingPerIssuerPoi(int issuerCount) =>
		issuerCount > 1 ? System.Math.Max(1, TargetGeneratedCount - 1) : int.MaxValue;
}
