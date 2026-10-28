using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public enum Icon
{
	Damage,
	Hull,
	Shield,
	Heat,
	Overdrive,
	Overheat,
	ActionPoints,
	ManeuverPoints,
	Cooldown,
}

// Battle copy keeps Icon.* token names; content model is shared.
public sealed record BattleHudTooltipContent(
	string? Title,
	string Body,
	string? Details = null)
{
	public static BattleHudTooltipContent Plain(string text) =>
		new(null, text);

	public string TriggerText => Title ?? Body;

	public HudTooltipContent ToShared() =>
		new(Title, Body, Details);

	public static implicit operator HudTooltipContent(BattleHudTooltipContent content) =>
		content.ToShared();
}
