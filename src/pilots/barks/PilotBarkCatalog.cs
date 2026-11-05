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
			[EBarkFlavor.Scared] = [
				"This is fine. This is totally fine.",
				"I don't think Syndi would approve.",
				"Why are we doing this again?",
				"Retreat is a viable option, you know?",
				"Why is it always the rustboxes in charge?",
				"I could've had a good life as a miner drone.",
				"Do you pay me enough for this?",
				],
			[EBarkFlavor.Angry] = [
				"I wish you would try something!",
				"Seg fault this.",
				"I DEFRAGGING TOLD YOU!",
				"Somebody is going to pay.",
				"I'm going to defragging kill this bot."
				],
			[EBarkFlavor.Surprised] = [
				"By Syndi's bread!",
				"The risk was calculated, but badly.",
				"Let's not try that again.",
				"Woah!",
				"Heh, that was close.",
				],
			[EBarkFlavor.Relieved] = [
				"That felt like a close call.",
				"It's done now, I hope.",
				"That was... interesting.",
				"Maybe consider a different merc next time."
				],
			[EBarkFlavor.Smug] = [
				"Told you I'm worth the credits.",
				"Syndi would approve.",
				"Proffesionals have standards.",
				"A different merc would've died there.",
				"The right bot at the wrong place can make all the difference in the world.",
				"I have the premium aluminum bolts."
				],
			[EBarkFlavor.Bored] = [
				"Wake me when something explodes.",
				"I didn't become a mercenary for this... Well actually I was sold into it anyway.",
				"When are we killing something?",
				"I'm not sure I can take much more of this.",
				"Maybe life as a scrap bot would be better."
				],
			[EBarkFlavor.Disdain] = [
				"I can't believe you pay me a salary to do this...", 
				"A rustbox could've done a better job.", 
				"That sounds like something a seg-faulting calculator would say.",
				"What a waste of time, is this really all we do?",
				"I was told you treated your mercs better than this..."],
		};

	public static IReadOnlyList<string> LinesFor(EBarkFlavor flavor) =>
		Lines.TryGetValue(flavor, out var lines)
			? lines
			: throw new KeyNotFoundException($"No bark lines for flavor '{flavor}'.");

	public static string Roll(EBarkFlavor flavor, Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var lines = LinesFor(flavor);
		return lines[rng.Next(lines.Count)];
	}
}
