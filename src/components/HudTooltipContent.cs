namespace GrimSpace.Components;

public sealed record HudTooltipContent(
	string? Title,
	string Body,
	string? Details = null)
{
	public static HudTooltipContent Plain(string text) =>
		new(null, text);

	public string TriggerText => Title ?? Body;
}
