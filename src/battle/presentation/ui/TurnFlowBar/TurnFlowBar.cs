using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class TurnFlowBar : Control
{
	public const double HandoffSeconds = 0.7;
	private readonly CenteredCarousel _carousel;
	private readonly TurnFlowDivider[] _turnDividers =
		new TurnFlowDivider[CenteredCarousel.VisibleSlots + 1];
	private IReadOnlyList<TurnFlowEntry> _timeline = [];

	public event Action<string>? UnitClicked;
	public bool IsAnimating => _carousel.IsAnimating;

	public TurnFlowBar()
	{
		MouseFilter = MouseFilterEnum.Stop;
		_carousel = new CenteredCarousel();
		CustomMinimumSize = _carousel.CustomMinimumSize;
		_carousel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_carousel.ItemClicked += unitId => UnitClicked?.Invoke(unitId);
		_carousel.IndexChanged += _ => UpdateTurnDividers();
		AddChild(_carousel);

		for (var gap = 0; gap < _turnDividers.Length; gap++)
		{
			var divider = new TurnFlowDivider();
			_carousel.AddMovingOverlay(divider);
			_turnDividers[gap] = divider;
		}
	}

	public void SetTimeline(IReadOnlyList<TurnFlowEntry> timeline)
	{
		if (_timeline.SequenceEqual(timeline))
			return;

		_timeline = timeline;
		_carousel.SetItems(timeline
			.Select(entry => entry.UnitId is null
				? null
				: new CenteredCarouselEntry(entry.UnitId, entry.UnitId))
			.ToArray());
		UpdateTurnDividers();
	}

	public void SetPortraitResolver(Func<string, Texture2D?> portraitFor) =>
		_carousel.SetIconResolver(portraitFor);

	public void SetPortraitTintResolver(Func<string, Color> portraitTintFor) =>
		_carousel.SetIconTintResolver(portraitTintFor);

	public void SetUnitVisualResolver(Func<string, Control?> visualFor) =>
		_carousel.SetItemVisualResolver(visualFor);

	public void SetIndex(int index, bool changeActive = true)
	{
		_carousel.SetIndex(index, changeActive);
		UpdateTurnDividers();
	}

	public void AnimateHandoff(int index, Action? completed = null) =>
		_carousel.AnimateHandoff(index, HandoffSeconds, completed);

	private void UpdateTurnDividers()
	{
		for (var gap = 0; gap < _turnDividers.Length; gap++)
		{
			var visibleGap = gap - 1;
			var activationIndex = _carousel.ItemIndexForSlot(visibleGap);
			var show = TurnFlowTimeline.HasDividerAfter(_timeline, activationIndex);
			if (!show)
			{
				_turnDividers[gap].HideDivider();
				continue;
			}

			_turnDividers[gap].ShowAt(
				_carousel.GapMarkerPosition(visibleGap),
				TurnFlowTimeline.CompletedTurnsToLeft(_timeline, activationIndex));
		}
	}
}
