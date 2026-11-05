namespace GrimSpace.Pilots.Barks;

public static class PilotBarkCatalog
{
	public static IReadOnlyList<string> OpeningLines { get; } = Array.AsReadOnly<string>(
	[
		"Oh, Syndi's beard, WHAT. A. RUSTBOX. this system is, how did I even end up here?",
		"Oh right, the drugs...",
	]);

	// TODO: placeholder copy, one stub line per flavor.
	private static readonly IReadOnlyDictionary<EBarkFlavor, IReadOnlyList<string>> Lines =
		new Dictionary<EBarkFlavor, IReadOnlyList<string>>
		{
			[EBarkFlavor.Scared] = ["This is fine. This is totally fine."],
			[EBarkFlavor.Angry] = ["Somebody is paying for that."],
			[EBarkFlavor.Surprised] = ["Huh. Didn't see that coming."],
			[EBarkFlavor.Relieved] = ["Still breathing. Good."],
			[EBarkFlavor.Smug] = ["Told you I'm worth the credits."],
			[EBarkFlavor.Bored] = ["Wake me when something explodes."],
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
