using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class ContractHudOverlay : Control
{
	private enum ViewMode
	{
		List,
		Details,
		DeclineConfirm,
	}

	private readonly ModalShell _shell;
	private StarMap _map = null!;
	private string _activePoiId = "";
	private string _facilityTitle = "";
	private Contract? _selected;
	private ViewMode _mode = ViewMode.List;
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

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

		_shell = new ModalShell(HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(StarMap map, string activePoiId, string facilityTitle)
	{
		_map = map;
		_activePoiId = activePoiId;
		_facilityTitle = facilityTitle;
		_selected = null;
		_statusKind = null;
		_statusMessage = "";
		_mode = ViewMode.List;
		_shell.Open(_facilityTitle, "Select an offer");
		ShowList();
	}

	public void Close()
	{
		_selected = null;
		_statusKind = null;
		_statusMessage = "";
		_shell.Close();
	}

	public void SyncMap(StarMap map) => _map = map;

	public void ShowError(string message)
	{
		_statusKind = HudStatusKind.Error;
		_statusMessage = message;
		_selected = null;
		ShowList();
	}

	public void ShowConfirmation(string message, HudStatusKind kind)
	{
		_statusKind = kind;
		_statusMessage = message;
		_selected = null;
		ShowList();
	}

	private void ShowList()
	{
		_mode = ViewMode.List;
		var contracts = _map.ContractRegistry.AvailableForPoi(_activePoiId).ToArray();
		_shell.SetTitle(_facilityTitle);
		_shell.SetSubtitle(contracts.Length == 0
			? "No contract offers available"
			: $"{contracts.Length} contract {Pluralize(contracts.Length, "offer", "offers")} available");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		if (_selected is not null
			&& !_map.ContractRegistry.AvailableForPoi(_activePoiId).Any(contract => contract.Id == _selected.Id))
			_selected = null;

		var body = HudWidgets.CreateCardList();

		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage));

		if (contracts.Length == 0 && _statusKind is null)
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

		_statusKind = null;
		_statusMessage = "";
		_mode = ViewMode.Details;
		_shell.SetTitle(ContractDisplay.Title(_selected));
		_shell.SetSubtitle($"Issued by {ContractDisplay.Issuer(_selected, _map)}");
		_shell.SetHeader(HudHeaderMode.Back, ShowList);
		_shell.SetBackHandler(ShowList);

		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", HudStyles.HalfMargin);

		body.AddChild(HudWidgets.CreateSection("Mission Briefing", ContractDisplay.Narrative(_selected)));
		body.AddChild(HudWidgets.CreateSection("Objective", ContractDisplay.ObjectivePreview(_selected)));
		body.AddChild(HudWidgets.CreateSection("Expected Opposition", ContractDisplay.ForceEstimate(_selected)));
		body.AddChild(HudWidgets.CreateSection("Search Area", ContractDisplay.SearchArea(_selected)));
		body.AddChild(HudWidgets.CreateSection(
			"Compensation",
			ContractDisplay.Reward(_selected),
			bodyRole: HudTextRole.Success));
		body.AddChild(HudWidgets.CreateSection(
			"Threat Assessment",
			ContractDisplay.Danger(_selected),
			bodyRole: ThreatTextRole(_selected)));

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction("Back", HudActionKind.Secondary, ShowList),
			new HudAction("Decline", HudActionKind.Destructive, ShowDeclineConfirm),
			new HudAction("Accept", HudActionKind.Primary, OnAcceptPressed),
		]);
	}

	private void ShowDeclineConfirm()
	{
		if (_selected is null || !TryRefreshSelected())
		{
			ShowList();
			return;
		}

		_mode = ViewMode.DeclineConfirm;
		_shell.SetTitle(ContractDisplay.Title(_selected));
		_shell.SetSubtitle("Confirm decline");
		_shell.SetHeader(HudHeaderMode.Back, ShowDetails);
		_shell.SetBackHandler(ShowDetails);

		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddChild(HudWidgets.CreateWarningPanel(
			$"Permanently decline \"{ContractDisplay.Title(_selected)}\" for this run?"));

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction("Cancel", HudActionKind.Secondary, ShowDetails),
			new HudAction("Confirm decline", HudActionKind.Destructive, OnConfirmDeclinePressed),
		]);
	}

	private Control CreateContractCard(Contract contract)
	{
		var rows = new List<HudTextLine>
		{
			new($"DIFFICULTY  {ContractDisplay.DifficultyStars(contract)}", HudTextRole.Metadata),
		};

		return HudWidgets.CreateCard(
			ContractDisplay.Title(contract),
			rows,
			() =>
			{
				_selected = contract;
				ShowDetails();
			});
	}

	private static HudTextRole ThreatTextRole(Contract contract)
	{
		if (!ContractDisplay.TryGetDangerLevel(contract, out var danger))
			return HudTextRole.Metadata;

		return danger switch
		{
			EDangerLevel.VeryLow => HudTextRole.Metadata,
			_ => HudTextRole.Danger,
		};
	}

	private static string Pluralize(int count, string singular, string plural) =>
		count == 1 ? singular : plural;

	private bool TryRefreshSelected()
	{
		if (_selected is null)
			return false;

		if (!_map.ContractRegistry.TryGet(_selected.Id, out var contract)
			|| !_map.ContractRegistry.IsOffered(_selected.Id))
			return false;

		_selected = contract;
		return true;
	}

	private void OnAcceptPressed()
	{
		if (_selected is null)
			return;

		AcceptRequested?.Invoke(_selected.Id);
	}

	private void OnConfirmDeclinePressed()
	{
		if (_selected is null)
			return;

		DeclineRequested?.Invoke(_selected.Id);
	}
}
