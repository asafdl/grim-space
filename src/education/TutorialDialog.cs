using Godot;
using GrimSpace.Components;

namespace GrimSpace.Education;

public sealed partial class TutorialDialog : MarginContainer, ITutorialDialog
{
	private readonly RichTextLabel _message;
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

		_message = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			SelectionEnabled = false,
			ContextMenuEnabled = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ThemeTypeVariation = "TutorialRichTextLabel",
			MetaUnderlined = true,
		};
		_message.AddThemeFontSizeOverride("normal_font_size", 15);
		_message.MetaClicked += OnMetaClicked;
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

	public event Action<string>? WorldLinkClicked;

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

	private void OnMetaClicked(Variant metadata)
	{
		if (metadata.VariantType != Variant.Type.String)
		{
			GD.PushError($"Tutorial received unsupported link metadata type '{metadata.VariantType}'.");
			return;
		}

		WorldLinkClicked?.Invoke(metadata.AsString());
	}
}
