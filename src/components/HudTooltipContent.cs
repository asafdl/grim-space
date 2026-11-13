namespace GrimSpace.Components;

public sealed record HudTooltipRow(
	string Label,
	string Value,
	string? IconName = null,
	int? Delta = null,
	string DeltaSuffix = "");

public sealed record HudTooltipInlineRow(string? IconName, IReadOnlyList<HudTooltipRow> Values);

public sealed record HudTooltipComparison(HudTooltipContent Current, HudTooltipContent Candidate);

public sealed record HudTooltipContent(
	string? Title,
	string Body,
	string? Details = null)
{
	/// <summary>Splits formatted Details/Body text into sections separated by a rule.</summary>
	public const string Divider = "[divider]";

	public IReadOnlyDictionary<string, HudTooltipContent>? IconTooltips { get; init; }

	public IReadOnlyList<HudTooltipRow>? Rows { get; init; }

	public IReadOnlyList<HudTooltipInlineRow>? InlineRows { get; init; }

	public bool TwoColumnRows { get; init; }

	public HudTooltipComparison? Comparison { get; init; }

	public static HudTooltipContent Plain(string text) =>
		new(null, text);

	public string TriggerText => Title ?? Body;
}
