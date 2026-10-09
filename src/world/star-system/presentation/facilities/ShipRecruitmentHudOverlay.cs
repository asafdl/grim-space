using Godot;
using GrimSpace.Components;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed partial class ShipRecruitmentHudOverlay : Control
{
	private readonly ModalShell _shell;
	private State _run = null!;
	private string _facilityTitle = "";
	private HudStatusKind? _statusKind;
	private string _statusMessage = "";

	public event Action<ShipRecruitmentCatalog.Offer>? RecruitmentRequested;
	public event Action? Closed;

	public ShipRecruitmentHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetDismissVisible(true);
		_shell.Closed += () => Closed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(State run, string facilityTitle)
	{
		_run = run;
		_facilityTitle = facilityTitle;
		_statusKind = null;
		_statusMessage = "";
		_shell.Open("Ships for hire", _facilityTitle);
		ShowMain();
	}

	public void ShowError(string message)
	{
		_statusKind = HudStatusKind.Error;
		_statusMessage = message;
		ShowMain();
	}

	public void Sync(State run)
	{
		_run = run;
		if (IsOpen)
			ShowMain();
	}

	public void ShowConfirmation(string message)
	{
		_statusKind = HudStatusKind.Success;
		_statusMessage = message;
		ShowMain();
	}

	private void ShowMain()
	{
		_shell.SetTitle("Ships for hire");
		_shell.SetSubtitle(_facilityTitle);
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = HudWidgets.CreateCardList();
		body.AddThemeConstantOverride("separation", 18);

		if (_statusKind is not null && !string.IsNullOrEmpty(_statusMessage))
			body.AddChild(HudWidgets.CreateStatusPanel(_statusKind.Value, _statusMessage));

		var shipCount = _run.PlayerParty.ShipIds.Count;
		body.AddChild(HudWidgets.CreateStatusPanel(
			HudStatusKind.Neutral,
			$"Fleet roster: {shipCount}/{EnlistPlayerShipActionDef.MaxPlayerShips} ships"));

		if (shipCount >= EnlistPlayerShipActionDef.MaxPlayerShips)
		{
			body.AddChild(HudWidgets.CreateStatusPanel(
				HudStatusKind.Warning,
				"Your fleet roster is full."));
			_shell.SetBody(body);
			return;
		}

		foreach (var offer in ShipRecruitmentCatalog.All)
		{
			var captured = offer;
			var canAfford = _run.StarSystem.Map.PlayerResources.CanApply(captured.Cost.Negate());
			var availability = canAfford
				? "Ready to join your fleet."
				: "You do not have the required resources.";
			body.AddChild(HudWidgets.CreateCard(
				DisplayName(captured.Chassis),
				[ResourceCostDisplay.CreateOfferDetails(availability, captured.Cost)],
				() => RecruitmentRequested?.Invoke(captured)));
		}

		_shell.SetBody(body);
	}

	private static string DisplayName(EType chassis) =>
		chassis switch
		{
			EType.RepurposedMiner => "Repurposed Miner",
			EType.Gunship => "Gunship",
			_ => chassis.ToString(),
		};
}
