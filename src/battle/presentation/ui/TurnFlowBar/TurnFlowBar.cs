using Godot;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class TurnFlowBar : Control
{
	private readonly CenteredCarousel _carousel;
	private readonly TurnFlowDivider[] _turnDividers =
		new TurnFlowDivider[CenteredCarousel.VisibleSlots - 1];
	private IReadOnlyList<TurnFlowEntry> _timeline = [];

	public event Action<string>? UnitClicked;

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
			_carousel.AddViewportOverlay(divider);
			_turnDividers[gap] = divider;
		}
	}

	public void SetTimeline(IReadOnlyList<TurnFlowEntry> timeline)
	{
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

	public void SetIndex(int index, bool changeActive = true)
	{
		_carousel.SetIndex(index, changeActive);
		UpdateTurnDividers();
	}

	private void UpdateTurnDividers()
	{
		for (var gap = 0; gap < _turnDividers.Length; gap++)
		{
			var activationIndex = _carousel.ItemIndexForSlot(gap);
			var show = TurnFlowTimeline.HasDividerAfter(_timeline, activationIndex);
			if (!show)
			{
				_turnDividers[gap].HideDivider();
				continue;
			}

			_turnDividers[gap].ShowAt(
				_carousel.GapMarkerPosition(gap),
				TurnFlowTimeline.CompletedTurnsToLeft(_timeline, activationIndex));
		}
	}
}
