using Godot;
using GrimSpace.Presentation.Ui.Hud;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class ContractHudOverlay : Node
{
	private enum ViewMode
	{
		List,
		Details,
		DeclineConfirm,
	}

	private readonly ModalHudShell _shell;
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
		_shell = new ModalHudShell();
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
		_shell.SetTitle(_facilityTitle);
		_shell.SetSubtitle("Select an offer");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		if (_selected is not null
			&& !_map.ContractRegistry.AvailableForPoi(_activePoiId).Any(contract => contract.Id == _selected.Id))
			_selected = null;

		var viewport = _shell.GetViewport();
		var body = HudWidgets.CreateCardList(viewport);

		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage, viewport));

		var contracts = _map.ContractRegistry.AvailableForPoi(_activePoiId).ToArray();
		if (contracts.Length == 0 && _statusKind is null)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Neutral,
				"No contracts available from this authority.",
				viewport));
		}
		else
		{
			foreach (var contract in contracts)
			{
				var captured = contract;
				body.AddChild(CreateContractCard(captured, viewport));
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
		_shell.SetTitle(_facilityTitle);
		_shell.SetSubtitle(ContractDisplay.Title(_selected));
		_shell.SetHeader(HudHeaderMode.Back, ShowList);
		_shell.SetBackHandler(ShowList);

		var viewport = _shell.GetViewport();
		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", HudStyles.Margin(viewport) / 2);

		body.AddChild(HudWidgets.CreateSection("Briefing", ContractDisplay.Narrative(_selected), scrollBody: true, viewport));
		body.AddChild(HudWidgets.CreateSection("Objective", ContractDisplay.ObjectiveSummary(_selected), viewport: viewport));
		body.AddChild(HudWidgets.CreateSection("Location", ContractDisplay.SearchArea(_selected), viewport: viewport));
		body.AddChild(HudWidgets.CreateSection(
			"Compensation",
			ContractDisplay.Reward(_selected),
			viewport: viewport,
			bodyRole: HudTextRole.Success));
		body.AddChild(HudWidgets.CreateSection(
			"Threat",
			ContractDisplay.Danger(_selected),
			viewport: viewport,
			bodyRole: DangerTextRole(_selected)));

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
		_shell.SetTitle(_facilityTitle);
		_shell.SetSubtitle("Confirm decline");
		_shell.SetHeader(HudHeaderMode.Back, ShowDetails);
		_shell.SetBackHandler(ShowDetails);

		var viewport = _shell.GetViewport();
		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddChild(HudWidgets.CreateWarningPanel(
			$"Permanently decline \"{ContractDisplay.Title(_selected)}\" for this run?",
			viewport));

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction("Cancel", HudActionKind.Secondary, ShowDetails),
			new HudAction("Confirm decline", HudActionKind.Destructive, OnConfirmDeclinePressed),
		]);
	}

	private Control CreateContractCard(Contract contract, Viewport viewport)
	{
		var dangerRole = DangerTextRole(contract);

		var rows = new List<HudTextLine>
		{
			new($"Issuer: {ContractDisplay.Issuer(contract, _map)}", HudTextRole.Metadata),
			new(ContractDisplay.Reward(contract), HudTextRole.Emphasis, HudStyles.TextColor(HudTextRole.Success)),
			new($"Danger: {ContractDisplay.Danger(contract)}", dangerRole),
			new(ContractDisplay.ObjectivePreview(contract), HudTextRole.Metadata),
		};

		return HudWidgets.CreateCard(
			ContractDisplay.Title(contract),
			rows,
			() =>
			{
				_selected = contract;
				ShowDetails();
			},
			viewport);
	}

	private static HudTextRole DangerTextRole(Contract contract) =>
		ContractDisplay.TryGetDangerLevel(contract, out var danger) && danger != EDangerLevel.VeryLow
			? HudTextRole.Danger
			: HudTextRole.Warning;

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
