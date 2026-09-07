using Godot;

namespace GrimSpace.Presentation.Ui.Hud;

public sealed partial class ModalHudShell : CanvasLayer
{
	private Control _root = null!;
	private PanelContainer _panel = null!;
	private MarginContainer _outer = null!;
	private Label _title = null!;
	private Label _subtitle = null!;
	private Button _headerButton = null!;
	private ScrollContainer _bodyScroll = null!;
	private VBoxContainer _bodyHost = null!;
	private HBoxContainer _footer = null!;

	private HudHeaderMode _headerMode = HudHeaderMode.Close;
	private Action? _headerAction;
	private Action? _backHandler;
	private Action? _closeHandler;
	private IReadOnlyList<HudAction> _footerActions = [];
	private Control _headerRow = null!;

	public event Action? Closed;

	public ModalHudShell()
	{
		Layer = 20;
		Build();
		Visible = false;
	}

	public bool IsOpen => Visible;

	public void Open(string title, string subtitle = "")
	{
		SetTitle(title);
		SetSubtitle(subtitle);
		Visible = true;
		LayoutPanel();
		Callable.From(LayoutPanel).CallDeferred();
	}

	public void Close()
	{
		Visible = false;
		_backHandler = null;
		_closeHandler = null;
		Closed?.Invoke();
	}

	private void CloseWithHandler()
	{
		_closeHandler?.Invoke();
		Close();
	}

	public void SetTitle(string title) => _title.Text = title;

	public void SetSubtitle(string subtitle)
	{
		_subtitle.Text = subtitle;
		_subtitle.Visible = !string.IsNullOrEmpty(subtitle);
	}

	public void SetHeader(HudHeaderMode mode, Action? onHeaderPressed = null)
	{
		_headerMode = mode;
		_headerAction = onHeaderPressed;
		_headerButton.Text = mode == HudHeaderMode.Close ? "×" : "←";
	}

	public void SetBackHandler(Action? handler) => _backHandler = handler;

	public void SetCloseHandler(Action? handler) => _closeHandler = handler;

	public void SetHeaderVisible(bool visible) => _headerRow.Visible = visible;

	public void SetBody(Control content)
	{
		foreach (var child in _bodyHost.GetChildren())
			child.QueueFree();

		content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_bodyHost.AddChild(content);
	}

	public void SetFooter(IReadOnlyList<HudAction> actions)
	{
		_footerActions = actions;
		RebuildFooter();
	}

	private void RebuildFooter()
	{
		foreach (var child in _footer.GetChildren())
			child.QueueFree();

		_footer.Visible = _footerActions.Count > 0;
		if (_footerActions.Count == 0)
			return;

		var secondary = _footerActions.Where(action => action.Kind == HudActionKind.Secondary).ToArray();
		var primary = _footerActions.Where(action => action.Kind != HudActionKind.Secondary).ToArray();

		foreach (var action in secondary)
			_footer.AddChild(CreateFooterButton(action));

		var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_footer.AddChild(spacer);

		foreach (var action in primary)
			_footer.AddChild(CreateFooterButton(action));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		if (_backHandler is not null)
			_backHandler();
		else
			CloseWithHandler();

		GetViewport().SetInputAsHandled();
	}

	private void Build()
	{
		_root = new Control
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		AddChild(_root);

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
			Color = HudStyles.ModalBackdrop,
		};
		_root.AddChild(backdrop);

		var center = new CenterContainer
		{
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Both,
		};
		_root.AddChild(center);

		_panel = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		HudStyles.SetPanelVariation(_panel, "Shell");
		center.AddChild(_panel);

		_outer = new MarginContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		_outer.AddThemeConstantOverride("margin_left", HudStyles.Margin);
		_outer.AddThemeConstantOverride("margin_right", HudStyles.Margin);
		_outer.AddThemeConstantOverride("margin_top", HudStyles.Margin);
		_outer.AddThemeConstantOverride("margin_bottom", HudStyles.Margin);
		_panel.AddChild(_outer);

		var layout = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
		};
		layout.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		_outer.AddChild(layout);

		_headerRow = BuildHeader();
		layout.AddChild(_headerRow);

		_bodyScroll = new ScrollContainer
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		layout.AddChild(_bodyScroll);

		_bodyHost = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_bodyHost.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		_bodyScroll.AddChild(_bodyHost);

		_footer = new HBoxContainer();
		_footer.AddThemeConstantOverride("separation", 10);
		_footer.Visible = false;
		layout.AddChild(_footer);

		Callable.From(ConnectViewportLayout).CallDeferred();
	}

	private Control BuildHeader()
	{
		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 12);

		_headerButton = new Button
		{
			Text = "×",
			Flat = true,
			FocusMode = Control.FocusModeEnum.All,
		};
		HudStyles.StyleButton(_headerButton, HudActionKind.Secondary);
		_headerButton.Pressed += OnHeaderPressed;
		row.AddChild(_headerButton);

		var titles = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		titles.AddThemeConstantOverride("separation", 2);

		_title = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = "Title",
		};
		titles.AddChild(_title);

		_subtitle = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = "Subtitle",
		};
		titles.AddChild(_subtitle);

		row.AddChild(titles);
		return row;
	}

	private void OnHeaderPressed()
	{
		if (_headerMode == HudHeaderMode.Close)
		{
			CloseWithHandler();
			return;
		}

		if (_headerAction is not null)
			_headerAction();
		else
			_backHandler?.Invoke();
	}

	private Button CreateFooterButton(HudAction action)
	{
		var button = new Button
		{
			Text = action.Label,
			Disabled = !action.Enabled,
			FocusMode = Control.FocusModeEnum.All,
		};
		HudStyles.StyleButton(button, action.Kind);
		button.Pressed += action.OnPressed;
		return button;
	}

	private void ConnectViewportLayout()
	{
		GetViewport().SizeChanged += OnViewportSizeChanged;
		LayoutPanel();
	}

	private void OnViewportSizeChanged() => LayoutPanel();

	private void LayoutPanel()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		var width = Mathf.Clamp(
			720f,
			viewportSize.X * 0.38f,
			Mathf.Min(viewportSize.X * 0.58f, 800f));
		var height = Mathf.Clamp(
			540f,
			viewportSize.Y * 0.55f,
			viewportSize.Y * 0.85f);

		_panel.CustomMinimumSize = new Vector2(width, height);
		RebuildFooter();
	}
}
