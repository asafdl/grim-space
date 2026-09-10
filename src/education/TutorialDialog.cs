using Godot;
using GrimSpace.Components;

namespace GrimSpace.Education;

public sealed partial class TutorialDialog : MarginContainer, ITutorialDialog
{
	private readonly Label _message;
	private readonly Button _accept;

	public TutorialDialog()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;

		var panel = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Stop,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.TutorialDialogPanelType,
		};
		AddChild(panel);

		var content = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		content.AddThemeConstantOverride("separation", 10);
		panel.AddChild(content);

		_message = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "BodyLabel",
		};
		_message.AddThemeFontSizeOverride("font_size", 15);
		content.AddChild(_message);

		_accept = new Button
		{
			CustomMinimumSize = new Vector2(104, 34),
			FocusMode = FocusModeEnum.All,
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
		};
		HudStyles.StyleButton(_accept, HudActionKind.Secondary);
		_accept.AddThemeFontSizeOverride("font_size", 14);
		_accept.Pressed += () => Accepted?.Invoke();
		content.AddChild(_accept);
	}

	public event Action? Accepted;

	public bool IsOpen => Visible;

	public override void _Ready() => HudThemes.Apply(this, HudThemeFamily.Tutorial);

	public void Open(TutorialDialogContent content)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentException.ThrowIfNullOrEmpty(content.Title);
		ArgumentException.ThrowIfNullOrEmpty(content.Message);
		ArgumentException.ThrowIfNullOrEmpty(content.AcceptText);

		_message.Text = content.Message;
		_accept.Text = content.AcceptText;
		Visible = true;
		_accept.GrabFocus();
	}

	public void Close() => Visible = false;
}
