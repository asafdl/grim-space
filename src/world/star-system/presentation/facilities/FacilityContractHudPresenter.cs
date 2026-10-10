using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class FacilityContractHudPresenter
{
	private readonly FacilitySceneBinding _binding;
	private readonly ContractHudOverlay _hud;
	private readonly Action _stateChanged;

	public FacilityContractHudPresenter(
		Control owner,
		FacilitySceneBinding binding,
		Action stateChanged)
	{
		ArgumentNullException.ThrowIfNull(owner);
		ArgumentNullException.ThrowIfNull(binding);
		ArgumentNullException.ThrowIfNull(stateChanged);

		_binding = binding;
		_stateChanged = stateChanged;
		var layer = new CanvasLayer { Layer = 20 };
		owner.AddChild(layer);
		_hud = new ContractHudOverlay();
		_hud.AcceptRequested += OnAcceptRequested;
		_hud.DeclineRequested += OnDeclineRequested;
		_hud.Closed += OnClosed;
		layer.AddChild(_hud);
	}

	public bool IsOpen => _hud.IsOpen;

	public void Open(FacilityOperator facilityOperator)
	{
		if (!_binding.TryVisitContractOperator(facilityOperator.Name))
		{
			GD.PushError(
				$"Unable to pause contract placement for operator '{facilityOperator.Name}' " +
				$"at POI '{_binding.PoiId}'.");
			return;
		}

		_hud.Open(
			_binding.Map,
			_binding.PoiId,
			_binding.FacilityId,
			facilityOperator.Name,
			OperatorDisplayLabels.Title(facilityOperator));
		_stateChanged();
	}

	public void SyncMap() => _hud.SyncMap(_binding.Map);

	private void OnAcceptRequested(string contractId)
	{
		if (!_binding.Intents.TryAcceptContract(contractId))
		{
			_hud.ShowError("Unable to accept contract.");
			_stateChanged();
			return;
		}

		_hud.SyncMap(_binding.Map);
		_hud.ShowConfirmation("Contract accepted.", HudStatusKind.Success);
		_stateChanged();
	}

	private void OnDeclineRequested(string contractId)
	{
		if (!_binding.Intents.TryDeclineContract(contractId))
		{
			_hud.ShowError("Unable to decline contract.");
			_stateChanged();
			return;
		}

		_hud.SyncMap(_binding.Map);
		_hud.ShowConfirmation("Contract declined.", HudStatusKind.Error);
		_stateChanged();
	}

	private void OnClosed()
	{
		MapNavigationContext.ClearActiveOperator();
		_stateChanged();
	}

}
