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

	private readonly CenteredCarouselSlot[] _slots = new CenteredCarouselSlot[VisibleSlots];
	private readonly Control _viewport;
	private IReadOnlyList<CenteredCarouselEntry?> _items = [];
	private int _index;
	private int _highlightedIndex;
	private Func<string, Texture2D?> _iconFor = _ => null;

	public event Action<string>? ItemClicked;
	public event Action<int>? IndexChanged;

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
		_viewport.AddChild(row);

		for (var slot = 0; slot < VisibleSlots; slot++)
		{
			var slotView = new CenteredCarouselSlot(SlotSize);
			slotView.ItemClicked += itemId => ItemClicked?.Invoke(itemId);
			row.AddChild(slotView);
			_slots[slot] = slotView;
		}

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

	public void SetIndex(int index, bool changeHighlight = true)
	{
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

	public int ItemIndexForSlot(int slot) =>
		CenteredCarouselWindow.ItemIndexForSlot(_index, slot, CenterSlot);

	public void AddViewportOverlay(Control overlay) => _viewport.AddChild(overlay);

	public Vector2 GapMarkerPosition(int gap)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(gap);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(gap, VisibleSlots - 1);
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

		SetIndex(
			_index + (mouse.ButtonIndex == MouseButton.WheelUp ? -1 : 1),
			changeHighlight: false);
		GetViewport().SetInputAsHandled();
	}

	private void Render()
	{
		for (var slot = 0; slot < VisibleSlots; slot++)
		{
			var itemIndex = ItemIndexForSlot(slot);
			var item = CenteredCarouselWindow.ItemAt<CenteredCarouselEntry>(_items, itemIndex);
			if (item is null)
			{
				_slots[slot].ShowEmpty();
				continue;
			}

			var icon = _iconFor(item.Id);
			var highlighted = itemIndex == _highlightedIndex;
			var opacity = slot == CenterSlot - 1 || slot == CenterSlot + 1 ? 0.85f : 0.55f;
			_slots[slot].ShowItem(item, icon, highlighted, opacity);
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
