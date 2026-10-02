using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class TurnFlowBar : PanelContainer
{
	private const int VisibleSlots = 5;
	private const int CenterSlot = 2;
	private const int BoxSize = 64;
	private const int ViewportWidth = 380;
	private const int ViewportHeight = 80;
	private const int Separation = 12;

	private readonly TurnFlowSlot[] _slots = new TurnFlowSlot[VisibleSlots];
	private readonly TurnFlowDivider[] _turnDividers = new TurnFlowDivider[VisibleSlots - 1];
	private HBoxContainer _row = null!;

	private IReadOnlyList<TurnFlowEntry> _timeline = [];
	private int _index;
	private int _activeIndex;
	private int _renderedIndex;
	private Func<string, Texture2D?> _portraitFor = _ => null;

	public event Action<string>? UnitClicked;

	public TurnFlowBar()
	{
		CustomMinimumSize = new Vector2(ViewportWidth + 16, ViewportHeight + 8);
		MouseFilter = MouseFilterEnum.Stop;
		ClipContents = true;
		AddThemeStyleboxOverride("panel", MakePanelStyle());

		var viewport = new Control
		{
			CustomMinimumSize = new Vector2(ViewportWidth, ViewportHeight),
			MouseFilter = MouseFilterEnum.Stop,
			ClipContents = true,
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};

		_row = new HBoxContainer
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
		_row.AddThemeConstantOverride("separation", Separation);
		viewport.AddChild(_row);

		for (var slot = 0; slot < VisibleSlots; slot++)
		{
			var slotView = new TurnFlowSlot();
			slotView.UnitClicked += unitId => UnitClicked?.Invoke(unitId);
			_row.AddChild(slotView);
			_slots[slot] = slotView;
		}

		for (var gap = 0; gap < VisibleSlots - 1; gap++)
		{
			var divider = new TurnFlowDivider();
			viewport.AddChild(divider);
			_turnDividers[gap] = divider;
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
		content.AddChild(viewport);
	}

	public void SetTimeline(IReadOnlyList<TurnFlowEntry> timeline)
	{
		_timeline = timeline;
		_index = TurnFlowTimeline.ClampIndex(_index, _timeline.Count);
		_activeIndex = TurnFlowTimeline.ClampIndex(_activeIndex, _timeline.Count);
		_renderedIndex = _index;
		Render();
	}

	public void SetPortraitResolver(Func<string, Texture2D?> portraitFor)
	{
		_portraitFor = portraitFor;
		Render();
	}

	public void SetIndex(int index, bool changeActive = true)
	{
		var nextIndex = TurnFlowTimeline.ClampIndex(index, _timeline.Count);
		if (changeActive)
			_activeIndex = nextIndex;

		if (nextIndex == _index)
		{
			if (changeActive)
				Render();
			return;
		}

		_index = nextIndex;
		_renderedIndex = _index;
		Render();
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
			changeActive: false);
		GetViewport().SetInputAsHandled();
	}

	private void Render()
	{
		for (var slot = 0; slot < VisibleSlots; slot++)
		{
			var box = _slots[slot];
			var activationIndex = TurnFlowTimeline.ActivationIndexForSlot(
				_renderedIndex,
				slot,
				CenterSlot);
			var unitId = TurnFlowTimeline.UnitAt(_timeline, activationIndex);
			if (unitId is null)
			{
				box.ShowEmpty();
				continue;
			}

			var portrait = _portraitFor(unitId);
			var isActing = activationIndex == _activeIndex;
			var opacity = slot == CenterSlot - 1 || slot == CenterSlot + 1 ? 0.85f : 0.55f;
			box.ShowUnit(unitId, portrait, isActing, opacity);
		}

		UpdateTurnDividers();
	}

	private void UpdateTurnDividers()
	{
		var rowWidth = (VisibleSlots * BoxSize) + ((VisibleSlots - 1) * Separation);
		var rowLeft = (ViewportWidth - rowWidth) * 0.5f;
		var rowTop = (ViewportHeight - BoxSize) * 0.5f;
		for (var gap = 0; gap < VisibleSlots - 1; gap++)
		{
			var activationIndex = TurnFlowTimeline.ActivationIndexForSlot(
				_renderedIndex,
				gap,
				CenterSlot);
			var show = TurnFlowTimeline.HasDividerAfter(_timeline, activationIndex);
			if (!show)
			{
				_turnDividers[gap].HideDivider();
				continue;
			}

			var gapCenterX = rowLeft
				+ ((gap + 1) * BoxSize)
				+ (gap * Separation)
				+ (Separation * 0.5f);
			_turnDividers[gap].ShowAt(
				new Vector2(gapCenterX - 1f, rowTop - 4f),
				TurnFlowTimeline.CompletedTurnsToLeft(_timeline, activationIndex));
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
