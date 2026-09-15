using Godot;
using GrimSpace.Components;

namespace GrimSpace.Education;

public sealed partial class TutorialDialog : MarginContainer, ITutorialDialog
{
	private readonly RichTextLabel _message;
	private readonly VBoxContainer _assistance;
	private readonly Label _assistanceMessage;
	private readonly Button _assistanceAction;
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

		_assistance = new VBoxContainer
		{
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_assistance.AddThemeConstantOverride("separation", 8);
		content.AddChild(_assistance);

		_assistanceMessage = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = "TutorialRichTextLabel",
		};
		_assistanceMessage.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.28f));
		_assistance.AddChild(_assistanceMessage);

		_assistanceAction = new Button
		{
			CustomMinimumSize = new Vector2(144, 34),
			FocusMode = FocusModeEnum.All,
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
		};
		HudStyles.StyleButton(_assistanceAction, HudActionKind.Secondary);
		_assistanceAction.AddThemeFontSizeOverride("font_size", 14);
		_assistanceAction.Pressed += () => AssistanceRequested?.Invoke();
		_assistance.AddChild(_assistanceAction);

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

	public event Action? AssistanceRequested;

	public event Action<string>? WorldLinkClicked;

	public bool IsOpen => Visible;

	public override void _Ready() => HudThemes.Apply(this, HudThemeFamily.Tutorial);

	public void Open(TutorialDialogContent content)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentException.ThrowIfNullOrEmpty(content.Title);
		ArgumentException.ThrowIfNullOrEmpty(content.Message);

		ClearAssistance();
		_message.Text = content.Message;
		_accept.Visible = content.AcceptText is not null;
		if (content.AcceptText is { } acceptText)
			_accept.Text = acceptText;
		else
			_accept.ReleaseFocus();
		Visible = true;
		if (_accept.Visible)
			_accept.GrabFocus();
	}

	public void ShowAssistance(TutorialAssistanceContent content)
	{
		ArgumentNullException.ThrowIfNull(content);
		ArgumentException.ThrowIfNullOrEmpty(content.Message);
		ArgumentException.ThrowIfNullOrEmpty(content.ActionText);

		_assistanceMessage.Text = content.Message;
		_assistanceAction.Text = content.ActionText;
		_assistance.Visible = true;
		_assistanceAction.GrabFocus();
	}

	public void ClearAssistance()
	{
		_assistance.Visible = false;
		_assistanceAction.ReleaseFocus();
	}

	public void Close()
	{
		ClearAssistance();
		Visible = false;
	}

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
