namespace GrimSpace.Components;

public sealed record HudTooltipContent(
	string? Title,
	string Body,
	string? Details = null)
{
	/// <summary>Splits formatted Details/Body text into sections separated by a rule.</summary>
	public const string Divider = "[divider]";

	public IReadOnlyDictionary<string, HudTooltipContent>? IconTooltips { get; init; }

	public static HudTooltipContent Plain(string text) =>
		new(null, text);

	public string TriggerText => Title ?? Body;
}
