namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public enum ELeadKind
{
	BlackMarketSale,
}

public interface ILeadObjective : IContractObjective
{
	ELeadKind LeadKind { get; }
	int DurationTicks { get; }
}
