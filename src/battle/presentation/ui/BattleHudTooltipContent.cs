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

public sealed record BattleHudTooltipContent(
	string? Title,
	string Body,
	string? Details = null)
{
	public static BattleHudTooltipContent Plain(string text) =>
		new(null, text);

	public string TriggerText => Title ?? Body;
}
