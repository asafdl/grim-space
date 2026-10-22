using Godot;
using GrimSpace.Ui.Components;

namespace GrimSpace.Components;

public sealed partial class ModalShell : Control
{
	private UiContainer _container = null!;
	private MarginContainer _outer = null!;
	private Label _title = null!;
	private Label _subtitle = null!;
	private UiButton _headerButton = null!;
	private UiButton _dismissButton = null!;
	private ScrollContainer _bodyScroll = null!;
	private VBoxContainer _bodyHost = null!;
	private HBoxContainer _footer = null!;

	private HudHeaderMode _headerMode = HudHeaderMode.Close;
	private Action? _headerAction;
	private Action? _backHandler;
	private Action? _closeHandler;
	private bool _dismissVisible;
	private IReadOnlyList<HudAction> _footerActions = [];
	private Control _headerRow = null!;
	private MarginContainer _bodyMargin = null!;
	private readonly HudThemeFamily? _themeFamily;
	private readonly UiContainerVisual _containerVisual;
	private UiButtonVisual _footerButtonVisual = UiButtonVisual.Action;
	private Vector2? _panelMinimumSizeOverride;

	public event Action? Closed;

	public ModalShell(HudThemeFamily? themeFamily = null)
		: this(UiContainerVisual.Modal, themeFamily)
	{
	}

	public ModalShell(
		UiContainerVisual containerVisual,
		HudThemeFamily? themeFamily = null)
	{
		_containerVisual = containerVisual;
		_themeFamily = themeFamily;
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		Build();
		Visible = false;
	}

	public bool IsOpen => GodotObject.IsInstanceValid(this) && Visible;

	public void Open(string title, string subtitle = "")
	{
		SetTitle(title);
		SetSubtitle(subtitle);
		Visible = true;
		CallDeferred(MethodName.DeferredLayoutPanel);
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

	public void SetSubtitle(string subtitle) => _subtitle.Text = subtitle ?? "";

	public void SetHeader(
		HudHeaderMode mode,
		Action? onHeaderPressed = null,
		string? buttonLabel = null)
	{
		_headerMode = mode;
		_headerAction = onHeaderPressed;
		if (mode == HudHeaderMode.Close)
		{
			_headerButton.SetVisual(UiButtonVisual.Compact);
			_headerButton.Text = buttonLabel ?? "×";
		}
		else
		{
			_headerButton.SetVisual(UiButtonVisual.Back);
			_headerButton.Text = buttonLabel ?? HudWidgets.LeaveButtonLabel;
		}

		_headerButton.Visible = mode == HudHeaderMode.Back || !_dismissVisible;
	}

	public void SetDismissVisible(bool visible)
	{
		_dismissVisible = visible;
		_dismissButton.Visible = visible && _headerRow.Visible;
		_headerButton.Visible = _headerMode == HudHeaderMode.Back || !visible;
	}

	public void SetBackHandler(Action? handler) => _backHandler = handler;

	public void SetCloseHandler(Action? handler) => _closeHandler = handler;

	public void SetHeaderVisible(bool visible)
	{
		_headerRow.Visible = visible;
		_dismissButton.Visible = visible && _dismissVisible;
	}

	public void SetBody(Control content)
	{
		foreach (var child in _bodyHost.GetChildren())
			child.QueueFree();

		content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_bodyHost.AddChild(content);
	}

	public void SetFooter(IReadOnlyList<HudAction> actions)
	{
		_footerActions = actions;
		RebuildFooter();
	}

	public void SetFooterButtonVisual(UiButtonVisual visual)
	{
		_footerButtonVisual = visual;
		RebuildFooter();
	}

	public void SetPanelMinimumSize(Vector2 minimumSize)
	{
		_panelMinimumSizeOverride = minimumSize;
		if (IsInsideTree())
			LayoutPanel();
	}

	private void RebuildFooter()
	{
		foreach (var child in _footer.GetChildren())
			child.QueueFree();

		_footer.Visible = _footerActions.Count > 0;
		if (_footerActions.Count == 0)
			return;

		var secondary = _footerActions.Where(action =>
			action.Kind is HudActionKind.Secondary or HudActionKind.Destructive).ToArray();
		var primary = _footerActions.Where(action => action.Kind == HudActionKind.Primary).ToArray();

		foreach (var action in secondary)
			_footer.AddChild(CreateFooterButton(action));

		var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_footer.AddChild(spacer);

		foreach (var action in primary)
			_footer.AddChild(CreateFooterButton(action));
	}

	public bool TryHandleInput(InputEvent @event)
	{
		if (!Visible
			|| @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return false;

		HandleEscape();
		return true;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!TryHandleInput(@event))
			return;

		GetViewport().SetInputAsHandled();
	}

	private void HandleEscape()
	{
		if (_backHandler is not null)
			_backHandler();
		else
			CloseWithHandler();
	}

	private void Build()
	{
		MouseFilter = MouseFilterEnum.Stop;

		var backdrop = new ColorRect
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Stop,
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

		_container = new UiContainer
		{
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			CustomMinimumSize = ToGodotSize(ModalShellLayout.BaselinePanelMinimumSize),
		};
		_container.SetVisual(_containerVisual);
		center.AddChild(_container);

		if (_themeFamily is { } themeFamily)
			HudThemes.Apply(this, themeFamily);

		_outer = new MarginContainer
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		_container.AddChild(_outer);

		var layout = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		layout.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		_outer.AddChild(layout);

		_headerRow = BuildHeader();
		layout.AddChild(_headerRow);

		_bodyScroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		_bodyMargin = new MarginContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_bodyMargin.AddChild(_bodyScroll);
		layout.AddChild(_bodyMargin);

		_bodyHost = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_bodyHost.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		_bodyScroll.AddChild(_bodyHost);

		_footer = new HBoxContainer();
		_footer.AddThemeConstantOverride("separation", 10);
		_footer.Visible = false;
		layout.AddChild(_footer);

		ApplyContainerLayoutMetrics();
		Resized += OnResized;
	}

	private Control BuildHeader()
	{
		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 12);

		_headerButton = new UiButton("×", HudActionKind.Secondary, HudPalettes.BlueCyan)
		{
			FocusMode = FocusModeEnum.All,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
		};
		_headerButton.SetVisual(UiButtonVisual.Compact);
		_headerButton.Pressed += OnHeaderPressed;
		var leadingControls = new CenterContainer
		{
			CustomMinimumSize = new Vector2(_container.HeaderSideSlotWidth, 0f),
		};
		leadingControls.AddChild(_headerButton);
		row.AddChild(leadingControls);

		var titles = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		titles.AddThemeConstantOverride(
			"separation",
			_container.HeaderTitleSubtitleSeparation);

		_title = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "Title",
		};
		_title.AddThemeFontSizeOverride("font_size", _container.HeaderTitleFontSize);
		_title.AddThemeColorOverride("font_color", _container.HeaderTitleColor);
		var titleOffset = new MarginContainer();
		titleOffset.AddThemeConstantOverride("margin_top", _container.HeaderTitleOffsetY);
		titleOffset.AddThemeConstantOverride("margin_bottom", -_container.HeaderTitleOffsetY);
		titleOffset.AddChild(_title);
		titles.AddChild(titleOffset);

		var subtitleBand = new Control
		{
			CustomMinimumSize = new Vector2(0f, _container.HeaderSubtitleBandMinHeight),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_subtitle = new Label
		{
			AnchorsPreset = (int)LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "Subtitle",
		};
		_subtitle.AddThemeFontSizeOverride("font_size", _container.HeaderSubtitleFontSize);
		_subtitle.AddThemeColorOverride("font_color", _container.HeaderSubtitleColor);
		subtitleBand.AddChild(_subtitle);
		titles.AddChild(subtitleBand);

		row.AddChild(titles);
		var trailingControls = new Control
		{
			CustomMinimumSize = new Vector2(_container.HeaderSideSlotWidth, 0f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddChild(trailingControls);

		var dismissOffsets = _container.DismissButtonOffsets;
		_dismissButton = new UiButton("X", HudActionKind.Secondary, HudPalettes.BlueCyan)
		{
			Visible = false,
			AnchorLeft = 1f,
			AnchorRight = 1f,
			OffsetLeft = dismissOffsets.Position.X,
			OffsetTop = dismissOffsets.Position.Y,
			OffsetRight = dismissOffsets.End.X,
			OffsetBottom = dismissOffsets.End.Y,
		};
		_dismissButton.SetIntegrated(true);
		_dismissButton.SetFontSize(23);
		_dismissButton.Pressed += CloseWithHandler;
		_container.AddChild(_dismissButton);
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

	private UiButton CreateFooterButton(HudAction action)
	{
		var button = new UiButton(action.Label, action.Kind, HudPalettes.BlueCyan)
		{
			Disabled = !action.Enabled,
		};
		button.SetVisual(_footerButtonVisual);
		button.Pressed += () => action.OnPressed(button);
		return button;
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationEnterTree && Visible)
			CallDeferred(MethodName.DeferredLayoutPanel);
	}

	private void OnResized() => LayoutPanel();

	private void DeferredLayoutPanel() => LayoutPanel();

	private void LayoutPanel()
	{
		var viewportSize = IsInsideTree() ? GetViewportRect().Size : Vector2.Zero;
		var containerSize = ModalShellLayout.ResolveContainerSize(
			ToLayoutSize(Size),
			ToLayoutSize(viewportSize));
		_container.CustomMinimumSize = _panelMinimumSizeOverride
			?? ToGodotSize(ModalShellLayout.ComputePanelMinimumSize(containerSize));
		ApplyContainerLayoutMetrics();
		RebuildFooter();
	}

	private void ApplyContainerLayoutMetrics()
	{
		_outer.AddThemeConstantOverride("margin_left", _container.ContentMarginLeft);
		_outer.AddThemeConstantOverride("margin_right", _container.ContentMarginRight);
		_outer.AddThemeConstantOverride("margin_top", _container.ContentMarginTop);
		_outer.AddThemeConstantOverride("margin_bottom", _container.ContentMarginBottom);
		_bodyMargin.AddThemeConstantOverride("margin_top", _container.BodyMarginTop);
	}

	private static LayoutSize ToLayoutSize(Vector2 size) => new(size.X, size.Y);

	private static Vector2 ToGodotSize(LayoutSize size) => new(size.Width, size.Height);
}
