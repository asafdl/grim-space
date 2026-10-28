namespace GrimSpace.World.StarSystem.Resources;

public sealed record Transaction(string Source, ResourceBundle Change);

public static class TransactionSource
{
	public const string BattleLoot = "battle-loot";
	public const string ContractPayment = "contract-payment";
	public const string DebugGrant = "debug-grant";
	public const string MerchantPurchase = "merchant";
	public const string WreckageSalvage = "wreckage-salvage";
	public const string TravelFuel = "travel-fuel";
	public const string PilotSalary = "pilot-salary";
	public const string HazardPay = "hazard-pay";
	public const string FuelPurchase = "fuel-purchase";
}
