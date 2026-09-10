using Godot;

namespace GrimSpace.Components;

public sealed partial class FramedActionBar : Control
{
	private const int DefaultMinWidth = 720;
	private const int DefaultMaxWidth = 1100;
	private const float DefaultWidthRatio = 0.58f;
	private const int ActionWidth = 132;
	private const int ActionHeight = 52;

	private readonly MarginContainer _bottom;
	private readonly PanelContainer _frame;
	private readonly Label? _textBody;
	private readonly Button _action;

	private int _minWidth = DefaultMinWidth;
	private int _maxWidth = DefaultMaxWidth;
	private float _widthRatio = DefaultWidthRatio;

	public FramedActionBar()
		: this(CreateDefaultBody())
	{
	}

	public FramedActionBar(Control body)
	{
		ArgumentNullException.ThrowIfNull(body);
		_textBody = body as Label;
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		var layout = new VBoxContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		AddChild(layout);

		layout.AddChild(new Control
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
		});

		_bottom = new MarginContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_bottom.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		_bottom.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		layout.AddChild(_bottom);

		var center = new CenterContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_bottom.AddChild(center);

		_frame = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Stop,
		};
		HudStyles.SetPanelVariation(_frame, HudStyles.FramedActionBarFramePanelType);
		center.AddChild(_frame);

		var panel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Stop,
		};
		HudStyles.SetPanelVariation(panel, HudStyles.FramedActionBarPanelType);
		_frame.AddChild(panel);

		var content = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		content.AddThemeConstantOverride("separation", HudStyles.Margin);
		panel.AddChild(content);

		content.AddChild(body);

		var actionSlot = new CenterContainer
		{
			CustomMinimumSize = new Vector2(ActionWidth, ActionHeight),
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		content.AddChild(actionSlot);

		_action = new Button
		{
			Text = "Next",
			Visible = false,
			FocusMode = FocusModeEnum.All,
			CustomMinimumSize = new Vector2(ActionWidth, ActionHeight),
		};
		HudStyles.StyleButton(_action, HudActionKind.Secondary);
		_action.Pressed += () => ActionPressed?.Invoke();
		actionSlot.AddChild(_action);
	}

	public event Action? ActionPressed;

	public string Text
	{
		get => TextBody.Text;
		set => TextBody.Text = value;
	}

	public string ActionText
	{
		get => _action.Text;
		set => _action.Text = value;
	}

	public bool ActionVisible
	{
		get => _action.Visible;
		set => _action.Visible = value;
	}

	public bool ActionDisabled
	{
		get => _action.Disabled;
		set => _action.Disabled = value;
	}

	public void ConfigureWidth(int minWidth, int maxWidth, float widthRatio)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minWidth);
		ArgumentOutOfRangeException.ThrowIfLessThan(maxWidth, minWidth);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthRatio);

		_minWidth = minWidth;
		_maxWidth = maxWidth;
		_widthRatio = widthRatio;

		if (IsInsideTree())
			UpdateLayout();
	}

	public void FocusAction() => _action.GrabFocus();

	public bool ContainsGlobalPoint(Vector2 point) => _frame.GetGlobalRect().HasPoint(point);

	public override void _EnterTree()
	{
		GetViewport().SizeChanged += UpdateLayout;
		Callable.From(UpdateLayout).CallDeferred();
	}

	public override void _ExitTree() =>
		GetViewport().SizeChanged -= UpdateLayout;

	private Label TextBody =>
		_textBody
		?? throw new InvalidOperationException("This action bar uses a custom body control.");

	private static Label CreateDefaultBody() =>
		new()
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "BodyLabel",
		};

	private void UpdateLayout()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		var availableWidth = Mathf.Max(viewportSize.X - (HudStyles.Margin * 2), 0);
		var width = Mathf.Min(
			Mathf.Clamp(viewportSize.X * _widthRatio, _minWidth, _maxWidth),
			availableWidth);

		_frame.CustomMinimumSize = new Vector2(width, 0);
		_bottom.AddThemeConstantOverride(
			"margin_bottom",
			Mathf.RoundToInt(Mathf.Clamp(viewportSize.Y * 0.09f, 36f, 96f)));
	}
}
