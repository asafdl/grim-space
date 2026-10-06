using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts.Generation;

namespace GrimSpace.World.StarSystem.Contracts;

public static class DeliveryLegMessageCatalog
{
	public static string Pick(string contractId, int legIndex, string destination)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentOutOfRangeException.ThrowIfNegative(legIndex);
		ArgumentException.ThrowIfNullOrEmpty(destination);

		var seed = StableSeedMixer.From(0).Add(contractId).Add(legIndex).Add("delivery-next-leg-message").Value;
		var messages = ContractNarrativePickerConfig.DefaultEntries
			.Where(entry =>
				entry.Kind == EContractKind.Delivery
				&& entry.Subtype == ContractNarrativeSubtype.DeliveryLeg)
			.Select(entry => entry.Briefing)
			.ToArray();
		var index = (int)(seed % (ulong)messages.Length);
		return messages[index].Replace("{destination}", destination, StringComparison.Ordinal);
	}
}
