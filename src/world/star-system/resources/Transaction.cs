namespace GrimSpace.World.StarSystem.Resources;

public sealed record Transaction(string Source, ResourceBundle Change);

public static class TransactionSource
{
	public const string BattleLoot = "battle-loot";
	public const string ContractPayment = "contract-payment";
	public const string DockyardPurchase = "dockyard";
	public const string WreckageSalvage = "wreckage-salvage";
}
