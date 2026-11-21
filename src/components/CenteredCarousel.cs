using Godot;

namespace GrimSpace.Components;

public sealed record CenteredCarouselEntry(string Id, string Label);

public sealed partial class CenteredCarousel : PanelContainer
{
	public const int VisibleSlots = 5;
	public const int CenterSlot = 2;
	public const int SlotSize = 64;
	public const int ViewportWidth = 380;
	public const int ViewportHeight = 80;
	public const int Separation = 12;

	private readonly CenteredCarouselSlot[] _slots = new CenteredCarouselSlot[VisibleSlots + 2];
	private readonly Control _viewport;
	private readonly Control _strip;
	private readonly Panel _handoffBackdrop;
	private readonly Panel _handoffFocus;
	private readonly TextureRect _handoffCursor;
	private IReadOnlyList<CenteredCarouselEntry?> _items = [];
	private int _index;
	private int _highlightedIndex;
	private Func<string, Texture2D?> _iconFor = _ => null;
	private Func<string, Color> _iconTintFor = _ => Colors.White;
	private Func<string, Control?>? _itemVisualFor;
	private Tween? _handoffTween;
	private Action? _handoffCompleted;

	public event Action<string>? ItemClicked;
	public event Action<int>? IndexChanged;
	public bool IsAnimating => _handoffTween is not null;

	public CenteredCarousel()
	{
		CustomMinimumSize = new Vector2(ViewportWidth + 16, ViewportHeight + 8);
		MouseFilter = MouseFilterEnum.Stop;
		ClipContents = true;
		AddThemeStyleboxOverride("panel", MakePanelStyle());

		_viewport = new Control
		{
			CustomMinimumSize = new Vector2(ViewportWidth, ViewportHeight),
			MouseFilter = MouseFilterEnum.Stop,
			ClipContents = true,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		_handoffBackdrop = new Panel
		{
			Size = new Vector2(ViewportWidth, ViewportHeight),
			Modulate = new Color(1f, 1f, 1f, 0f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_handoffBackdrop.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.025f, 0.035f, 0.06f, 0.8f),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
		});
		_viewport.AddChild(_handoffBackdrop);
		_strip = new Control
		{
			Size = new Vector2(ViewportWidth, ViewportHeight),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_viewport.AddChild(_strip);

		var row = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			AnchorsPreset = (int)LayoutPreset.Center,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Both,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", Separation);
		_strip.AddChild(row);

		for (var slot = 0; slot < _slots.Length; slot++)
		{
			var slotView = new CenteredCarouselSlot(SlotSize);
			slotView.ItemClicked += itemId =>
			{
				if (!IsAnimating)
					ItemClicked?.Invoke(itemId);
			};
			row.AddChild(slotView);
			_slots[slot] = slotView;
		}
		_handoffFocus = new Panel
		{
			Position = new Vector2((ViewportWidth - SlotSize) * 0.5f, (ViewportHeight - SlotSize) * 0.5f),
			Size = Vector2.One * SlotSize,
			PivotOffset = Vector2.One * (SlotSize * 0.5f),
			Modulate = new Color(1f, 1f, 1f, 0f),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_handoffFocus.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = Colors.Transparent,
			BorderColor = new Color(1f, 0.78f, 0.4f, 0.9f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
		});
		_viewport.AddChild(_handoffFocus);
		_handoffCursor = new TextureRect
		{
			Texture = ResourceLoader.Load<Texture2D>("res://assets/ui/turn_flow/active_cursor.svg"),
			Position = new Vector2((ViewportWidth - 20f) * 0.5f, (ViewportHeight + SlotSize) * 0.5f - 2f),
			Size = new Vector2(20, 16),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_viewport.AddChild(_handoffCursor);

		var content = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		content.AddThemeConstantOverride("margin_left", 8);
		content.AddThemeConstantOverride("margin_right", 8);
		content.AddThemeConstantOverride("margin_top", 4);
		content.AddThemeConstantOverride("margin_bottom", 4);
		AddChild(content);
		content.AddChild(_viewport);
	}

	public void SetItems(IReadOnlyList<CenteredCarouselEntry?> items)
	{
		CancelHandoff();
		_items = items;
		_index = CenteredCarouselWindow.ClampIndex(_index, _items.Count);
		_highlightedIndex = CenteredCarouselWindow.ClampIndex(_highlightedIndex, _items.Count);
		Render();
	}

	public void SetIconResolver(Func<string, Texture2D?> iconFor)
	{
		_iconFor = iconFor;
		Render();
	}

	public void SetIconTintResolver(Func<string, Color> iconTintFor)
	{
		_iconTintFor = iconTintFor;
		Render();
	}

	public void SetItemVisualResolver(Func<string, Control?> itemVisualFor)
	{
		_itemVisualFor = itemVisualFor;
		foreach (var slot in _slots)
			slot.InvalidateItemVisual();
		Render();
	}

	public void SetIndex(int index, bool changeHighlight = true)
	{
		CancelHandoff();
		var nextIndex = CenteredCarouselWindow.ClampIndex(index, _items.Count);
		if (changeHighlight)
			_highlightedIndex = nextIndex;

		if (nextIndex == _index)
		{
			if (changeHighlight)
				Render();
			return;
		}

		_index = nextIndex;
		Render();
		IndexChanged?.Invoke(_index);
	}

	public void AnimateHandoff(int index, double duration, Action? completed = null)
	{
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, 0);
		CancelHandoff();
		var target = CenteredCarouselWindow.ClampIndex(index, _items.Count);
		var slide = target == _index + 1;
		if (!slide && target != _index)
			SetIndex(target);

		var current = _slots[CenterSlot + 1];
		var incoming = slide ? _slots[CenterSlot + 2] : current;
		foreach (var slot in _slots)
			slot.HideHighlightCursor();
		_handoffCursor.Visible = true;
		_handoffCompleted = completed;
		_handoffTween = CreateTween();
		_handoffTween.TweenProperty(_handoffBackdrop, "modulate:a", 1f, duration * 0.2);
		_handoffTween.Parallel().TweenProperty(_handoffFocus, "modulate:a", 1f, duration * 0.2);
		_handoffTween.Parallel().TweenProperty(_handoffFocus, "scale", Vector2.One * 1.25f, duration * 0.2);
		_handoffTween.Parallel().TweenProperty(current, "scale", Vector2.One * 1.25f, duration * 0.2);
		if (slide)
		{
			_handoffTween.TweenProperty(_strip, "position:x", -(SlotSize + Separation), duration * 0.55)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
			_handoffTween.Parallel().TweenProperty(current, "scale", Vector2.One, duration * 0.55)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
			_handoffTween.Parallel().TweenProperty(incoming, "scale", Vector2.One * 1.25f, duration * 0.55)
				.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
			_handoffTween.Parallel().TweenProperty(incoming, "modulate:a", 1f, duration * 0.55);
		}
		else
		{
			_handoffTween.TweenInterval(duration * 0.55);
		}
		_handoffTween.TweenProperty(_handoffBackdrop, "modulate:a", 0f, duration * 0.25);
		_handoffTween.Parallel().TweenProperty(_handoffFocus, "modulate:a", 0f, duration * 0.25);
		_handoffTween.Parallel().TweenProperty(_handoffFocus, "scale", Vector2.One, duration * 0.25);
		_handoffTween.Parallel().TweenProperty(incoming, "scale", Vector2.One, duration * 0.25);
		_handoffTween.TweenCallback(Callable.From(() => FinishHandoff(target)));
	}

	private void FinishHandoff(int target)
	{
		var completed = _handoffCompleted;
		_handoffCompleted = null;
		_handoffTween = null;
		_handoffCursor.Visible = false;
		_strip.Position = Vector2.Zero;
		SetIndex(target);
		completed?.Invoke();
	}

	private void CancelHandoff()
	{
		if (_handoffTween is null)
			return;

		_handoffTween.Kill();
		_handoffTween = null;
		_strip.Position = Vector2.Zero;
		_handoffBackdrop.Modulate = new Color(1f, 1f, 1f, 0f);
		_handoffFocus.Modulate = new Color(1f, 1f, 1f, 0f);
		_handoffFocus.Scale = Vector2.One;
		_handoffCursor.Visible = false;
		foreach (var slot in _slots)
			slot.Scale = Vector2.One;
		Render();
		var completed = _handoffCompleted;
		_handoffCompleted = null;
		if (completed is not null)
			Callable.From(completed).CallDeferred();
	}

	public override void _ExitTree()
	{
		_handoffCompleted = null;
		CancelHandoff();
		base._ExitTree();
	}

	public int ItemIndexForSlot(int slot) =>
		CenteredCarouselWindow.ItemIndexForSlot(_index, slot, CenterSlot);

	public void AddMovingOverlay(Control overlay) => _strip.AddChild(overlay);

	public Vector2 GapMarkerPosition(int gap)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(gap, -1);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(gap, VisibleSlots);
		var rowWidth = (VisibleSlots * SlotSize) + ((VisibleSlots - 1) * Separation);
		var rowLeft = (ViewportWidth - rowWidth) * 0.5f;
		var rowTop = (ViewportHeight - SlotSize) * 0.5f;
		var gapCenterX = rowLeft
			+ ((gap + 1) * SlotSize)
			+ (gap * Separation)
			+ (Separation * 0.5f);
		return new Vector2(gapCenterX - 1f, rowTop - 4f);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouse
			|| !mouse.Pressed
			|| mouse.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)
			|| !GetGlobalRect().HasPoint(mouse.GlobalPosition))
			return;

		if (IsAnimating)
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		SetIndex(
			_index + (mouse.ButtonIndex == MouseButton.WheelUp ? -1 : 1),
			changeHighlight: false);
		GetViewport().SetInputAsHandled();
	}

	private void Render()
	{
		for (var slot = 0; slot < _slots.Length; slot++)
		{
			var visibleSlot = slot - 1;
			var itemIndex = ItemIndexForSlot(visibleSlot);
			var item = CenteredCarouselWindow.ItemAt<CenteredCarouselEntry>(_items, itemIndex);
			if (item is null)
			{
				_slots[slot].ShowEmpty();
				continue;
			}

			var icon = _iconFor(item.Id);
			var iconTint = _iconTintFor(item.Id);
			var highlighted = itemIndex == _highlightedIndex;
			var opacity = visibleSlot == CenterSlot - 1 || visibleSlot == CenterSlot + 1 ? 0.85f : 0.55f;
			_slots[slot].ShowItem(item, icon, iconTint, highlighted, opacity, _itemVisualFor);
		}
	}

	private static StyleBoxFlat MakePanelStyle() =>
		new()
		{
			BgColor = new Color(0.06f, 0.08f, 0.12f, 0f),
			BorderColor = new Color(0.3f, 0.42f, 0.6f, 0.7f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomRight = 4,
			CornerRadiusBottomLeft = 4,
		};
}
