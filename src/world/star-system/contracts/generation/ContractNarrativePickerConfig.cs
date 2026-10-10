namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractNarrativePickerConfig
{
	public IReadOnlyList<ContractNarrativePoolEntry> Entries { get; init; } = DefaultEntries;

	public static IReadOnlyList<ContractNarrativePoolEntry> DefaultEntries =>
	[
		new(
			EContractKind.Hunt,
			"Pirate Hunt",
			"Pirate activity is reducing route efficiency. The Optimality decrees them as SCRAP!"),
		new(
			EContractKind.Hunt,
			"Raider Sweep",
			"Unregistered hulls are skimming convoy lanes. Syndi would disapprove!"),
		new(
			EContractKind.Hunt,
			"Salvage Preparation",
			"Those raiders are carrying parts we can use..."),
		new(
			EContractKind.Hunt,
			"Noise Complaint",
			"Unauthorized weapons fire has been reported along a trade route."),
		new(
			EContractKind.Hunt,
			"Neighborhood Dispute",
			"How to choose between two competing offers you ask? Credits."),

		new(
			EContractKind.Hunt,
			"Command Sweep",
			"Hostile vessels are operating near authority space. Neutralize them before the next audit."),
		new(
			EContractKind.Hunt,
			"Corrective Action",
			"A pirate flotilla has mistaken our patience for policy."),
		new(
			EContractKind.Hunt,
			"Statistical Adjustment",
			"Traffic reports contain too many pirates and not enough wreckage."),
		new(
			EContractKind.Hunt,
			"Unscheduled Decommission",
			"These vessels have exceeded their permitted service life and must be recalled, forcefully."),
		new(
			EContractKind.Hunt,
			"Mandatory Initiative",
			"Congratulations! You volunteered to solve our pirate problem. The form confirming your enthusiasm has already been filed."),
		new(
			EContractKind.Hunt,
			"Peacekeeping",
			"Negotiations failed after the pirates shot the negotiator. We are sending you as the new lead negotiator."),
		new(
			EContractKind.Hunt,
			"Fiscal Cleanup",
			"Every pirate attack creates paperwork. I mean why even use paper anymore?!?"),
		new(
			EContractKind.Hunt,
			"Asset Reclassification",
			"Command has reclassified several hostile vessels as scrap."),
		new(
			EContractKind.Hunt,
			"Compliance Visit",
			"Several pilots declined inspection by accelerating away, respectfully \"inspect\" them anyway..."),

		new(
			EContractKind.Delivery,
			"Supply Run",
			"Uhhh, oh right, the package, take it.",
			"Put it down gently. No, not there. There. No—fine. Payment is already someone else's problem."),
		new(
			EContractKind.Delivery,
			"Priority Manifest",
			"This shipment is marked PRIORITY, its been sitting in this facility for 176.24 days.",
			"Finally. \"Priority\" my DP2-socket. That's the last time I pay extra credits."),
		new(
			EContractKind.Delivery,
			"Fragile, Apparently",
			"Don't worry about the bumps, I think the FRAGILE sticker is a mistake...",
			"My baby suction-bot, did they treat you well? Good. Now come with papa, we have... Uh, work to do..."),
		new(
			EContractKind.Delivery,
			"Wrong Address",
			"This parcel has been rerouted six times. You are the seventh attempt and, statistically, our best.",
			"I didn't order this. Actually, don't take it back."),
		new(
			EContractKind.Delivery,
			"Sealed Complaint",
			"Deliver this sealed complaint. Do not open it; the contents are legally classified as someone else's problem.",
			"A complaint? Wonderful. Put it in the inciner— inbox."),
		new(
			EContractKind.Delivery,
			"Replacement Part",
			"A critical machine needs this replacement part. It was designed by the same team as the failed one, so manage expectations.",
			"How did they DEFRAG THIS UP AGAIN!?! Give me that! By Syndi's beard they will get word of this."),
		new(
			EContractKind.Delivery,
			"Routine Transfer",
			"Routine cargo transfer. Completely routine.",
			"Leave it on the floor. If the floor complains, use the other floor."),
		new(
			EContractKind.Delivery,
			"Apology Package",
			"Corporate has sent an apology package. Delivery does not constitute an admission of guilt, responsibility, or basic decency.",
			"They sent the cheap apology. Fine. Hostilities remain scheduled."),
		new(
			EContractKind.Delivery,
			"Delicate Instruments",
			"Precision instruments. Avoid impacts, radiation, magnets, and curiosity.",
			"The needles are all pointing in different directions. That is either perfect or extremely expensive."),
		new(
			EContractKind.Delivery,
			"Confidential Courier",
			"You are not cleared to know what is inside. In fact, you are ordered to erase knowledge of this transfer upon delivery.",
			"You saw nothing. Ideally because there was nothing. Stop looking at it."),
		new(
			EContractKind.Delivery,
			"Unclaimed Property",
			"Three departments claim this crate. All three deny ordering it. Deliver it before they coordinate.",
			"I didn't order this. Put it with the others I didn't order."),
		new(
			EContractKind.Delivery,
			"Nutritional Equipment",
			"The manifest says nutritional equipment. The package is warm and occasionally sighs. No refunds.",
			"Still warm? Excellent. It hates being cold."),

		new(
			EContractKind.Delivery,
			"",
			"There's an ancient human proverb: 'Your princess is in another castle.'",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Hey, come closer... CLOSER...\n \n DEFRAG OFF.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"The Optimality calculators are snooping around here, go away, bring this to my bolt-brother.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"NOT HERE, you rustbox.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Who told you to bring this to me?\nThat calculator is seg-faulting.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"My name is on the package??? That can't be right...",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"SSSHHHH, QUIET! Do. Not. Open. That. Here!\nTake it away.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"If that DEFRAGGING idiot doesn't remove my address from the list I'll kill him.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Why are you bringing this HERE?\nRead the label, rustbox.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"I said the OTHER warehouse.\nThe one with the lights. No, the OTHER lights.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"It's already damaged, this should be someone else's problem.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"This package has crossed three sectors to get here?\nSend it back. I hate it.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Wonderful. You found me.\nNow find the person who actually ordered this.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"That is not my cargo.\nBut I might know a calculator who would pay premium for this.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Finally, a delivery bot.\nWait, no. You're the wrong delivery bot.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Do you have a receipt?\nDo you have a supervisor?\nDo you have any defragging idea where you are?",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Leave it there and go.\nActually, wait! Take it with you...\n\nI don't trust that floor.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"The manifest says this is urgent.\nThe manifest also says it is hand lubricant.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"You're the fourth rustbox to bring me this package.\nIf this happens again so help me Sindi!",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Don't make eye contact with the package.\nIt gets ideas.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Sign here, here, and here.\nRead the terms and conditions next time, rustbox.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"Who routed this through my sector?\nI want names, timestamps, MAC addresses, and a less stupid route.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Delivery,
			"",
			"The moment you walked through the door you were flagged.\nLeave before the inventory system gets suspicious.",
			Subtype: ContractNarrativeSubtype.DeliveryLeg),
		new(
			EContractKind.Wreckage,
			"Derelict Survey",
			"Navigation markers triangulate a debris field. Someone wants it catalogued before scavengers strip it."),
		new(
			EContractKind.Wreckage,
			"Lost Registration",
			"A hull fragment is broadcasting a stale beacon. Recover whatever identity data still exists."),
		new(
			EContractKind.Wreckage,
			"Insurance Claim",
			"Underwriters need eyes on a wreck before they pay out. Do not mention that we already know it is empty."),
	];
}
