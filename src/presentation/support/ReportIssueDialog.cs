using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Support;

public sealed partial class ReportIssueDialog : Control
{
	private LineEdit _titleField = null!;
	private TextEdit _descriptionField = null!;
	private Label _status = null!;
	private Button _sendButton = null!;
	private Button _cancelButton = null!;
	private string _context = string.Empty;
	private bool _sending;
	private bool _limitingDescription;

	public bool IsOpen => Visible;

	public ReportIssueDialog()
	{
		Visible = false;
		ZIndex = 20;
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Stop;
		Build();
	}

	public void ApplyTheme(Theme theme) => Theme = theme;

	public void Open(string context)
	{
		_context = context;
		_titleField.Text = string.Empty;
		_descriptionField.Text = string.Empty;
		_status.Text = string.Empty;
		SetSending(false);
		Visible = true;
		_titleField.GrabFocus();
	}

	public void Close()
	{
		if (_sending)
			return;

		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || _sending)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Build()
	{
		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			Color = HudStyles.ModalBackdrop,
		};
		AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
		};
		AddChild(center);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(480, 0),
		};
		center.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var content = new VBoxContainer();
		content.AddThemeConstantOverride("separation", 12);
		margin.AddChild(content);

		content.AddChild(new Label
		{
			Text = ReportIssueCopy.DialogTitle,
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "OverlayTitle",
		});

		content.AddChild(new Label
		{
			Text = ReportIssueCopy.TitleField,
			ThemeTypeVariation = "EntryTitleLabel",
		});
		_titleField = new LineEdit
		{
			PlaceholderText = "Short summary",
			MaxLength = ReportIssueCopy.MaxTitleLength,
			CaretColumn = 0,
		};
		content.AddChild(_titleField);

		content.AddChild(new Label
		{
			Text = ReportIssueCopy.DescriptionField,
			ThemeTypeVariation = "EntryTitleLabel",
		});
		_descriptionField = new TextEdit
		{
			CustomMinimumSize = new Vector2(0, 140),
			PlaceholderText = "What happened? What did you expect?",
		};
		_descriptionField.TextChanged += LimitDescriptionLength;
		content.AddChild(_descriptionField);

		_status = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			ThemeTypeVariation = "EntryBodyLabel",
		};
		content.AddChild(_status);

		var actions = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.End,
		};
		actions.AddThemeConstantOverride("separation", 10);
		content.AddChild(actions);

		_sendButton = new Button
		{
			Text = ReportIssueCopy.Send,
			CustomMinimumSize = new Vector2(100, 40),
		};
		HudStyles.StyleButton(_sendButton, HudActionKind.Primary);
		_sendButton.Pressed += OnSendPressed;
		actions.AddChild(_sendButton);

		_cancelButton = new Button
		{
			Text = ReportIssueCopy.Cancel,
			CustomMinimumSize = new Vector2(100, 40),
		};
		HudStyles.StyleButton(_cancelButton, HudActionKind.Secondary);
		_cancelButton.Pressed += Close;
		actions.AddChild(_cancelButton);
	}

	private async void OnSendPressed()
	{
		if (_sending)
			return;

		var title = _titleField.Text.Trim();
		if (title.Length == 0)
		{
			_status.Text = ReportIssueCopy.TitleRequired;
			return;
		}

		SetSending(true);
		_status.Text = ReportIssueCopy.Sending;

		var description = _descriptionField.Text.Trim();
		var sent = await ReportIssueSender.TrySendAsync(_context, title, description);
		SetSending(false);

		if (!sent)
		{
			_status.Text = string.IsNullOrWhiteSpace(IssueReportChannels.DiscordWebhookUrl)
				? ReportIssueCopy.NotConfigured
				: ReportIssueCopy.SendFailed;
			return;
		}

		_status.Text = ReportIssueCopy.Sent;
		await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
		Close();
	}

	private void SetSending(bool sending)
	{
		_sending = sending;
		_sendButton.Disabled = sending;
		_cancelButton.Disabled = sending;
		_titleField.Editable = !sending;
		_descriptionField.Editable = !sending;
	}

	private void LimitDescriptionLength()
	{
		if (_limitingDescription
			|| _descriptionField.Text.Length <= ReportIssueCopy.MaxDescriptionLength)
			return;

		_limitingDescription = true;
		_descriptionField.Text = _descriptionField.Text[..ReportIssueCopy.MaxDescriptionLength];
		_limitingDescription = false;
	}
}
