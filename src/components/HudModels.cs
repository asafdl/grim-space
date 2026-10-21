using Godot;

namespace GrimSpace.Components;

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

public sealed record HudTextLine(string Text, HudTextRole Role = HudTextRole.Body, Color? ColorOverride = null);

public sealed record HudAction
{
	public HudAction(string label, HudActionKind kind, Action onPressed, bool enabled = true)
		: this(label, kind, _ => onPressed(), enabled)
	{
	}

	public HudAction(string label, HudActionKind kind, Action<BaseButton> onPressed, bool enabled = true)
	{
		Label = label;
		Kind = kind;
		OnPressed = onPressed;
		Enabled = enabled;
	}

	public string Label { get; }
	public HudActionKind Kind { get; }
	public Action<BaseButton> OnPressed { get; }
	public bool Enabled { get; }
}

public readonly record struct HudBannerView(Control Root, Label Title, Label Subtitle);

public readonly record struct HudPanelView(PanelContainer Root, VBoxContainer Body, Label? HeaderBadge = null);
