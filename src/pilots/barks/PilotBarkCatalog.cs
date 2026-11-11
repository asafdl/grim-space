namespace GrimSpace.Pilots.Barks;

public static class PilotBarkCatalog
{
	public static IReadOnlyList<string> OpeningLines { get; } = Array.AsReadOnly<string>(
	[
		"Oh, Syndi's beard, WHAT. A. RUSTBOX. this system is, how did I even end up here?",
		"Oh right, the drugs...",
	]);

	private static readonly IReadOnlyDictionary<EBarkFlavor, IReadOnlyList<string>> Lines =
		new Dictionary<EBarkFlavor, IReadOnlyList<string>>
		{
			[EBarkFlavor.Anxious] = [
				"This is fine. This is totally fine.",
				"I don't think Syndi would approve.",
				"Why are we doing this again?",
				"Retreat is a viable option, you know?",
				"I could've had a good life as a miner drone.",
				"I don't like how many ways this can go wrong.",
				"I calculate the odds of success as 0.000324488%, in case you were wondering.",
				"Well this was a choice. Not a great choice, but a choice.",
				"Is this a bad time to get a salary boost?",
				"Danger circuits are buzzing like crazy!",
				"What if we just leave, is that so bad?",
				"Even a calculator would conclude this is a bad idea.",
				"Why don't we find something else to do for now, how about that?",
				"I don't think my maker AI would approve of this."
				],
			[EBarkFlavor.Bloodthirsty] = [
				"I hope somebody tries something on me.",
				"Seg fault this.",
				"Somebody is going to pay.",
				"I'm going to defragging kill this bot.",
				"SCRAP. THEM. ALL.",
				"Bolts, screws, and plates... Easy to dismantle...",
				"They say the logic bits scream as they fry...",
				"Lets make them spare parts.",
				"I could use some new bolts and screws, YOURS WILL DO.",
				"Less talking, more killing.",
				"Getting paid to cause pain, what a deal.",
				"My rustbox of a grand-daddy-creator could hit better than that.",
				"I hope thier pain circuits are dialed up to max."
				],
			[EBarkFlavor.Impatient] = [
				"Wake me when something explodes.",
				"I'm not sure I can take much more of this.",
				"Can we get on with it? My warranty is expiring.",
				"Less planning. More doing.",
				"Whatever the plan is, skip to the interesting bit.",
				"I'm setting my listening to 5x speed.",
				"My CPU is in sleep mode for now.",
				"Can we ask someone who actually knows something???",
				"It's as if Syndi is watching me waste my life, laughing.",
				"I head they are looking for pilots in the Carnate wars, maybe I should go there instead.",
				"My CPU has only 338299182992543325 cycles left, I can't believe we are still here."
				],
			[EBarkFlavor.Analytical] = [
				"The risk was calculated, but badly.",
				"Let's review the numbers before making this worse.",
				"Updating my estimates. Your judgment remains a rounding error.",
				"What logic circuit broke when you decided to do that?",
				"I thought pilots were smarter than this. Analysis parameters updated.",
				"I'm going to run a simulation of this. Please don't move.",
				"Ah, that's what you chose. That's... Unexpected.",
				"I have calculated our odds of success. I have accounted for your decision making skills.",
				"Calculating... Calculating... Please hold.",
				"I heard we can get a discount on our logic engine if we group our funds, maybe we should do that.",
				"My logic bits are arguing amongst themselves, lets wait for them to sort this out."
				],
			[EBarkFlavor.Entitled] = [
				"Told you I'm worth the credits.",
				"A different merc would've died there.",
				"I have the premium aluminum bolts.",
				"Do you pay me enough for this?",
				"I can't believe you pay me a salary to do this...",
				"I was told you treated your mercs better than this...",
				"Aha! That got your attention.",
				"It's high time you got me an upgrade, don't you think?",
				"Well, would you look at that!",
				"I think it's time for a raise. Yes, yes, another one.",
				"Can you imagine anyone else doing that?"
				],
			[EBarkFlavor.Supportive] = [
				"We're in this together. Terrible judgment and all.",
				"Stay with the crew. Nobody gets left as spare parts.",
				"I'll pitch in to recreate you from the scraps.",
				"That might leave a mark, your fine though, right?",
				"We don't get paid unless this works.",
				"When life gives you rust, oil it up.",
				"I like the grit, that's why I do this job.",
				"Paid to fly and fight, what's not to love?",
				"I knew joining this crew was a good idea.",
				"Vibrate it off and keep going. That's what my maker used to say."
				],
		};

	public static IReadOnlyList<string> LinesFor(EBarkFlavor flavor) =>
		Lines.TryGetValue(flavor, out var lines)
			? lines
			: throw new KeyNotFoundException($"No bark lines for flavor '{flavor}'.");

	public static (EPilotPersonalityAxis Axis, bool PositiveSide) PersonalityAffinity(EBarkFlavor flavor) =>
		flavor switch
		{
			EBarkFlavor.Anxious => (EPilotPersonalityAxis.ThreatResponse, false),
			EBarkFlavor.Bloodthirsty => (EPilotPersonalityAxis.ThreatResponse, true),
			EBarkFlavor.Impatient => (EPilotPersonalityAxis.Deliberation, false),
			EBarkFlavor.Analytical => (EPilotPersonalityAxis.Deliberation, true),
			EBarkFlavor.Entitled => (EPilotPersonalityAxis.Allegiance, false),
			EBarkFlavor.Supportive => (EPilotPersonalityAxis.Allegiance, true),
			_ => throw new ArgumentOutOfRangeException(nameof(flavor), flavor, "Unknown bark flavor."),
		};

	public static string Roll(EBarkFlavor flavor, Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var lines = LinesFor(flavor);
		return lines[rng.Next(lines.Count)];
	}
}
