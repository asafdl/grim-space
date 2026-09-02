namespace GrimSpace.World.StarSystem.Areas;

public static class AreaIntelProducer
{
	private sealed record IntelLine(EAreaIntelTone Tone, string Template);

	private static readonly IntelLine[] LowLines =
	[
		new(EAreaIntelTone.Brief, "Between {A} and {B}."),
		new(EAreaIntelTone.Brief, "Somewhere along the {A}–{B} axis."),
		new(EAreaIntelTone.Brief, "On the connector between {A} and {B}."),
		new(EAreaIntelTone.Operational, "Search the segment between {A} and {B}."),
		new(EAreaIntelTone.Operational, "Target sits on the transit corridor linking {A} and {B}."),
		new(EAreaIntelTone.Operational, "Concentrate sweep along the {A} to {B} bearing."),
		new(EAreaIntelTone.Fragmentary, "Word is it's on the path from {A} to {B}."),
		new(EAreaIntelTone.Fragmentary, "Contact last seen somewhere between {A} and {B}."),
		new(EAreaIntelTone.Fragmentary, "They said it's between {A} and {B} — could be anywhere on that line."),
	];

	private static readonly IntelLine[] MedLines =
	[
		new(EAreaIntelTone.Brief, "Near {A} and {B}."),
		new(EAreaIntelTone.Brief, "In the vicinity of {A} and {B}."),
		new(EAreaIntelTone.Brief, "Close to both {A} and {B}, off the direct line."),
		new(EAreaIntelTone.Operational, "Off-axis but within supporting range of {A} and {B}."),
		new(EAreaIntelTone.Operational, "Search grid offset from the {A}/{B} corridor."),
		new(EAreaIntelTone.Operational, "Check the flanks near {A} and {B}."),
		new(EAreaIntelTone.Fragmentary, "Not on the main line, but not far from {A} and {B} either."),
		new(EAreaIntelTone.Fragmentary, "Rumor puts it near {A} and {B}, off to one side."),
		new(EAreaIntelTone.Fragmentary, "Near {A} and {B}, though the source wasn't precise."),
	];

	private static readonly IntelLine[] HighLines =
	[
		new(EAreaIntelTone.Brief, "Far from {A} and {B}."),
		new(EAreaIntelTone.Brief, "Well clear of the {A}–{B} line."),
		new(EAreaIntelTone.Brief, "Out on the periphery relative to {A} and {B}."),
		new(EAreaIntelTone.Operational, "Wide offset from the {A}–{B} baseline. Extend search perimeter."),
		new(EAreaIntelTone.Operational, "Target likely clear of the {A}/{B} corridor."),
		new(EAreaIntelTone.Operational, "Sweep the outer band — distant from both {A} and {B}."),
		new(EAreaIntelTone.Fragmentary, "Way out from {A} and {B}, if the tip is good."),
		new(EAreaIntelTone.Fragmentary, "Whoever gave this says it's far from both {A} and {B}."),
		new(EAreaIntelTone.Fragmentary, "Far from {A} and {B}. Take the lead with salt."),
	];

	public static string Produce(AreaIntelContext context) =>
		Produce(context, allowedTones: null);

	public static string Produce(AreaIntelContext context, IReadOnlyCollection<EAreaIntelTone>? allowedTones)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkADisplayName);
		ArgumentException.ThrowIfNullOrEmpty(context.LandmarkBDisplayName);

		var lines = LinesFor(context.Distance);
		if (allowedTones is not null)
		{
			if (allowedTones.Count == 0)
			{
				throw new ArgumentException(
					"allowedTones must not be empty when provided.",
					nameof(allowedTones));
			}

			lines = lines.Where(line => allowedTones.Contains(line.Tone)).ToArray();
		}

		if (lines.Length == 0)
		{
			throw new ArgumentException(
				"No intel lines match the requested distance and tone filter.",
				nameof(allowedTones));
		}

		var line = lines[Random.Shared.Next(lines.Length)];
		return line.Template
			.Replace("{A}", context.LandmarkADisplayName, StringComparison.Ordinal)
			.Replace("{B}", context.LandmarkBDisplayName, StringComparison.Ordinal);
	}

	private static IntelLine[] LinesFor(EAreaDistance distance) =>
		distance switch
		{
			EAreaDistance.Low => LowLines,
			EAreaDistance.Med => MedLines,
			EAreaDistance.High => HighLines,
			_ => throw new ArgumentOutOfRangeException(nameof(distance), distance, null),
		};
}
