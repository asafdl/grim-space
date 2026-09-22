using Godot;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Dialog;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class FacilityNpcDialogPresenter
{
	private readonly Facility _facility;
	private readonly StarMap _map;
	private readonly Button _backButton;
	private readonly NpcDialogHudOverlay _hud;
	private FacilityOperator? _operator;
	private int _rollIndex;

	public FacilityNpcDialogPresenter(Control owner, Button backButton, Facility facility, StarMap map)
	{
		_facility = facility;
		_map = map;
		_backButton = backButton;

		_hud = new NpcDialogHudOverlay();
		owner.AddChild(_hud);
		_hud.ChoiceSelected += OnChoiceSelected;
		_hud.Closed += OnHudClosed;
	}

	public bool IsOpen => _hud.IsOpen;

	public void Open(FacilityOperator facilityOperator)
	{
		_operator = facilityOperator;
		_rollIndex = 0;
		_hud.Open(BuildIdleDialog());
		UpdateBackButton();
	}

	public bool TryHandleInput(InputEvent @event) => _hud.TryHandleInput(@event);

	private void OnChoiceSelected(string choiceId)
	{
		if (_operator is null)
			return;

		if (choiceId == FacilityNpcDialogs.LeaveChoiceId)
		{
			_hud.Close();
			return;
		}

		if (choiceId == FacilityNpcDialogs.MoreChoiceId)
		{
			_rollIndex++;
			_hud.Open(BuildIdleDialog());
			return;
		}

		throw new InvalidOperationException($"Unknown NPC dialog choice '{choiceId}'.");
	}

	private NpcDialogDefinition BuildIdleDialog()
	{
		var facilityOperator = _operator
			?? throw new InvalidOperationException("NPC dialog requires an active facility operator.");
		return FacilityNpcDialogs.Idle(
			_facility,
			facilityOperator,
			_map.Seed,
			_map.Timeline.Clock.Current,
			_rollIndex);
	}

	private void OnHudClosed()
	{
		_operator = null;
		MapNavigationContext.ClearActiveOperator();
		UpdateBackButton();
	}

	private void UpdateBackButton() => _backButton.Disabled = _hud.IsOpen;
}
