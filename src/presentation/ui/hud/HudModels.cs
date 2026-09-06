using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public enum HudActionKind
{
	Secondary,
	Primary,
	Destructive,
}

public enum HudHeaderMode
{
	Close,
	Back,
}

public enum HudStatusKind
{
	Neutral,
	Success,
	Warning,
	Error,
}

public enum HudTextRole
{
	Metadata,
	Body,
	Emphasis,
	Success,
	Warning,
	Danger,
}

public enum HudFontRole
{
	Title,
	Subtitle,
	CardTitle,
	SectionHeading,
	Body,
	Metadata,
	Emphasis,
	Button,
	Status,
	HeaderControl,
}

public sealed record HudTextLine(string Text, HudTextRole Role = HudTextRole.Body, Color? ColorOverride = null);

public sealed record HudAction(
	string Label,
	HudActionKind Kind,
	Action OnPressed,
	bool Enabled = true);
