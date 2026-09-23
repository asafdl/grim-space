using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Poi.Dialog;

public static class FacilityIdleDialogLines
{
	private static readonly IReadOnlyDictionary<EPresentationAnchor, string[]> Pools =
		new Dictionary<EPresentationAnchor, string[]>
		{
			[EPresentationAnchor.Market] =
			[
				"Prices are what they are. Complain to the void.",
				"I've seen freighter captains cry over tariff sheets. Grow a spine or grow a wallet.",
				"Everything here is stolen twice before it hits the shelf. You want purity, die young.",
				"That smell? Profit. Don't ask which hold it came from.",
				"I don't care what you're buying. I care that you're paying.",
			],
			[EPresentationAnchor.Warehouse] =
			[
				"Labels lie. Pallets lie. I don't.",
				"Lost a whole crew in Bay Six once. Still billing their employer for storage.",
				"If it's leaking, it's classified. If it's screaming, it's perishable.",
				"Inventory is a religion. Heresy gets you forklifted into a crusher.",
				"You want your crate? Hope you brought a crowbar and a lawyer.",
			],
			[EPresentationAnchor.Refinery] =
			[
				"Everything that comes out of here used to be alive or someone's mistake.",
				"The vents sing at night. That's normal. The screaming isn't.",
				"We hit quota or we feed the slag furnace something soft.",
				"Don't breathe deep unless you like the taste of lawsuits.",
				"Ore doesn't care about your ethics. Neither do I.",
			],
			[EPresentationAnchor.Travel] =
			[
				"Jump math is clean. What waits on the other side isn't my department.",
				"Half the ships that use this gate don't file return manifests. Statistically.",
				"I've watched hulls come back inside-out. Ticket still non-refundable.",
				"Void doesn't negotiate. It just keeps whatever you forget to hold onto.",
				"Say your goodbyes before alignment. Saves time for the survivors.",
			],
			[EPresentationAnchor.Dockyard] =
			[
				"Your ship's ugly and I'm not fixing your personality.",
				"We weld over bloodstains. It's cheaper than decontamination.",
				"Torque specs are suggestions until something tears off in atmosphere.",
				"I've seen captains kiss their hull goodbye. Usually right before bankruptcy.",
				"Shields optional. Dignity not included.",
			],
			[EPresentationAnchor.Mine] =
			[
				"Ore doesn't care about your schedule. Neither does the quota board.",
				"That glow isn't ambiance. It's molten regret.",
				"We dig until something breaks. Usually a miner.",
				"Copper pays the bills. Blood pays the overtime.",
				"The manager drinks martinis. You drink recycled air.",
			],
			[EPresentationAnchor.Management] =
			[
				"Policy exists so someone else can be blamed when it fails.",
				"Compliance is just violence with paperwork.",
				"I sign forms. I don't read them. Neither should you.",
				"Your fleet is a line item. Try not to become a footnote.",
				"Administrative Core means we administrate until you break.",
			],
		};

	public static string Pick(EPresentationAnchor anchor, StableRandom random)
	{
		if (!Pools.TryGetValue(anchor, out var lines) || lines.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "No idle dialog pool for this facility.");

		var index = (int)(random.NextDouble() * lines.Length);
		if (index >= lines.Length)
			index = lines.Length - 1;

		return lines[index];
	}

	public static bool HasPool(EPresentationAnchor anchor) =>
		Pools.TryGetValue(anchor, out var lines) && lines.Length > 0;
}
