using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Education;

public sealed partial class TutorialDialog : MarginContainer, ITutorialDialog
{
	private static readonly HudPalette Palette = HudPalettes.Gold;

	private Tween? _tutorialPulse;
	private Tween? _acceptGlint;
	private readonly UiNestedContainer _panel;
	private readonly Label _tutorialTitle;
	private readonly Label _title;
	private readonly RichTextLabel _message;
	private readonly VBoxContainer _assistance;
	private readonly Label _assistanceMessage;
	private readonly UiButton _assistanceAction;
	private readonly UiButton _accept;

	public TutorialDialog()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;

		_panel = new UiNestedContainer(UiContainerVisual.Simple)
		{
			MouseFilter = MouseFilterEnum.Stop,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_panel.SetPalette(Palette);
		_panel.SetContentPadding(20, 14, 20, 14);
		AddChild(_panel);

		var content = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		content.AddThemeConstantOverride("separation", 8);
		_panel.SetContent(content);

		_tutorialTitle = new Label
		{
			Text = "TUTORIAL",
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = "CardTitle",
		};
		_tutorialTitle.AddThemeColorOverride("font_color", Palette.Title);
		_tutorialTitle.AddThemeFontSizeOverride("font_size", 18);
		content.AddChild(_tutorialTitle);

		_title = new Label
		{
			MouseFilter = MouseFilterEnum.Ignore,
			ThemeTypeVariation = "CardTitle",
		};
		content.AddChild(_title);

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
		_assistanceMessage.AddThemeColorOverride("font_color", Palette.Secondary);
		_assistance.AddChild(_assistanceMessage);

		_assistanceAction = CreateTutorialButton();
		_assistanceAction.Pressed += () => AssistanceRequested?.Invoke();
		_assistance.AddChild(_assistanceAction);

		_accept = CreateTutorialButton();
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
		_title.Text = content.Title;
		_message.Text = content.Message;
		_accept.Visible = content.AcceptText is not null;
		if (content.AcceptText is { } acceptText)
			_accept.Text = acceptText;
		else if (_accept.IsInsideTree())
			_accept.ReleaseFocus();
		Visible = true;
		StartTutorialPulse();
		if (_accept.Visible)
			StartAcceptGlint();
		else
			StopAcceptGlint();
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
		if (_assistanceAction.IsInsideTree())
			_assistanceAction.ReleaseFocus();
	}

	public void Close()
	{
		ClearAssistance();
		_tutorialPulse?.Kill();
		_tutorialPulse = null;
		StopAcceptGlint();
		Visible = false;
	}

	private static UiButton CreateTutorialButton()
	{
		var button = new UiButton("", HudActionKind.Secondary, Palette)
		{
			CustomMinimumSize = new Vector2(144, 40),
			FocusMode = FocusModeEnum.All,
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
		};
		button.SetVisual(UiButtonVisual.Compact);
		button.SetFontSize(14);
		return button;
	}

	private void StartTutorialPulse()
	{
		_tutorialPulse?.Kill();
		_tutorialTitle.Modulate = new Color(1f, 1f, 1f, 0.88f);
		_tutorialPulse = CreateTween()
			.SetLoops()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_tutorialPulse.TweenProperty(
			_tutorialTitle,
			"modulate",
			new Color(1f, 1f, 1f, 1f),
			1.5);
		_tutorialPulse.TweenProperty(
			_tutorialTitle,
			"modulate",
			new Color(1f, 1f, 1f, 0.88f),
			1.5);
	}

	private void StartAcceptGlint()
	{
		StopAcceptGlint();
		_accept.Modulate = new Color(1f, 1f, 1f, 0.86f);
		_acceptGlint = CreateTween()
			.SetLoops()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_acceptGlint.TweenProperty(
			_accept,
			"modulate",
			Colors.White,
			0.65);
		_acceptGlint.TweenProperty(
			_accept,
			"modulate",
			new Color(1f, 1f, 1f, 0.86f),
			0.65);
	}

	private void StopAcceptGlint()
	{
		_acceptGlint?.Kill();
		_acceptGlint = null;
		_accept.Modulate = Colors.White;
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
