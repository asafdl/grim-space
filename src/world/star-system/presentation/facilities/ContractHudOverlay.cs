using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class ContractHudOverlay : Control
{
	private readonly ModalShell _shell;
	private readonly CompactConfirmationPopup _declinePopup;
	private StarMap _map = null!;
	private string _activePoiId = "";
	private string _activeFacilityId = "";
	private string _activeOperatorName = "";
	private Contract? _selected;

	public event Action<string>? AcceptRequested;
	public event Action<string>? DeclineRequested;
	public event Action? Closed;

	public ContractHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(
			UiContainerVisual.Modal,
			HudThemeFamily.Informative);
		AddChild(_shell);
		_declinePopup = new CompactConfirmationPopup();
		_shell.AddChild(_declinePopup);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(
		StarMap map,
		string activePoiId,
		string activeFacilityId,
		string activeOperatorName)
	{
		_map = map;
		_activePoiId = activePoiId;
		_activeFacilityId = activeFacilityId;
		_activeOperatorName = activeOperatorName;
		_selected = null;
		_shell.Open("Contracts", "Select a contract");
		ShowList();
	}

	public void Close()
	{
		_declinePopup.Hide();
		_selected = null;
		_shell.Close();
	}

	public void SyncMap(StarMap map) => _map = map;

	/// <summary>Updates from latest map state and returns to the list. False when nothing left to show.</summary>
	public bool RefreshList(StarMap map)
	{
		_map = map;
		if (!IsOpen)
			return false;

		ShowList();
		return PresentedContracts().Length > 0;
	}

	private void ShowList()
	{
		_declinePopup.Hide();
		var contracts = PresentedContracts();
		_shell.SetTitle("Contracts");
		_shell.SetSubtitle(contracts.Length == 0
			? "No contracts available"
			: $"{contracts.Length} {Pluralize(contracts.Length, "contract", "contracts")} available");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		if (_selected is not null
			&& !contracts.Any(contract => contract.Id == _selected.Id))
			_selected = null;

		var body = HudWidgets.CreateCardList();

		if (contracts.Length == 0)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No contracts available from this authority."));
		}
		else
		{
			foreach (var contract in contracts)
			{
				var captured = contract;
				body.AddChild(CreateContractCard(captured));
			}
		}

		_shell.SetBody(body);
	}

	private void ShowDetails()
	{
		if (_selected is null || !TryRefreshSelected())
		{
			ShowList();
			return;
		}

		_declinePopup.Hide();
		_shell.SetTitle(ContractDisplay.TypeDisplayName(_selected));
		_shell.SetSubtitle(
			$"{ContractDisplay.Title(_selected)} · Issued by {ContractDisplay.Issuer(_selected)}");
		_shell.SetHeader(HudHeaderMode.Back, ShowList);
		_shell.SetBackHandler(ShowList);

		var details = new UiCard(
			ContractDisplay.Title(_selected),
			$"{ContractDisplay.KindDisplayName(ContractDisplay.Kind(_selected))}"
				+ $" · Issued by {ContractDisplay.Issuer(_selected)}",
			ContractDisplay.DetailsBody(_selected, _map, includeThreatAssessment: false),
			"")
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		details.SetVisual(UiCardVisual.Technical);
		details.SetInteractive(false);
		details.SetTrailingAlignment(HorizontalAlignment.Left);
		details.SetTrailingColor(HudPalettes.Gold.Primary);
		details.SetTrailingContent(BuildContractDetailsTrailing(_selected));

		_shell.SetBody(details);
		var footer = new List<HudAction>();
		if (_selected.AllowsDecline)
			footer.Add(new HudAction("Decline", HudActionKind.Destructive, ShowDeclineConfirm));
		footer.Add(new HudAction("Accept", HudActionKind.Primary, OnAcceptPressed));
		_shell.SetFooter(footer);
	}

	private void ShowDeclineConfirm(BaseButton anchor)
	{
		if (_selected is null || !TryRefreshSelected())
		{
			ShowList();
			return;
		}

		var contractId = _selected.Id;
		_declinePopup.Request(anchor, () => ConfirmDecline(contractId));
	}

	private Control CreateContractCard(Contract contract)
	{
		var item = new UiListItem(
			$"{ContractDisplay.ListTitle(contract)}",
			"",
			ContractDisplay.DifficultyStars(contract))
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		item.SetVisual(UiListItemVisual.Technical);
		item.SetTrailingColor(HudPalettes.Gold.Primary);
		if (!contract.Terms.Payment.IsEmpty)
			item.SetDetailBbcode(ResourceRewardDisplay.FormatListDetailBbcode(contract.Terms.Payment));
		item.Pressed += () =>
		{
			_selected = contract;
			ShowDetails();
		};
		return item;
	}

	private static Control BuildContractDetailsTrailing(Contract contract)
	{
		var trailing = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
		};
		trailing.AddThemeConstantOverride("separation", 8);

		if (!contract.Terms.Payment.IsEmpty)
		{
			var reward = new RichTextLabel
			{
				BbcodeEnabled = true,
				FitContent = true,
				ScrollActive = false,
				AutowrapMode = TextServer.AutowrapMode.Off,
				MouseFilter = MouseFilterEnum.Ignore,
				Text = ResourceRewardDisplay.FormatListDetailBbcode(contract.Terms.Payment),
				ThemeTypeVariation = HudStyles.InformativeItemDescriptionRichTextLabelType,
			};
			reward.AddThemeFontSizeOverride("normal_font_size", 16);
			trailing.AddChild(reward);
		}

		var threat = new Label
		{
			Text = $"THREAT // {ContractDisplay.DifficultyStars(contract)}",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		HudStyles.ApplyTextRole(threat, HudTextRole.Emphasis);
		trailing.AddChild(threat);
		return trailing;
	}

	private static string Pluralize(int count, string singular, string plural) =>
		count == 1 ? singular : plural;

	private bool TryRefreshSelected()
	{
		if (_selected is null)
			return false;

		var contract = PresentedContracts()
			.FirstOrDefault(candidate => candidate.Id == _selected.Id);
		if (contract is null)
			return false;

		_selected = contract;
		return true;
	}

	private Contract[] PresentedContracts()
	{
		if (!_map.TryGetPointOfInterest(_activePoiId, out var poi)
			|| !poi.OperatorTemporaryRoles.TryGetAssignment(
				_activeFacilityId,
				_activeOperatorName,
				out var assignment))
			return [];

		return assignment.SourceIds
			.Select(contractId =>
				_map.ContractRegistry.TryGet(contractId, out var contract)
					&& _map.ContractRegistry.IsPending(contractId)
					? contract
					: null)
			.Where(contract => contract is not null)
			.Cast<Contract>()
			.ToArray();
	}

	private void OnAcceptPressed()
	{
		if (_selected is null)
			return;

		AcceptRequested?.Invoke(_selected.Id);
	}

	private void ConfirmDecline(string contractId)
	{
		if (_selected?.Id != contractId || !TryRefreshSelected())
			return;

		DeclineRequested?.Invoke(contractId);
	}
}
