using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contact;
namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class EngagementHudOverlay : Control
{
	private readonly ModalShell _shell;
	private bool _busy;

	public event Action? EngageRequested;
	public event Action? FleeRequested;

	public EngagementHudOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(UiContainerVisual.Technical);
		_shell.SetFooterButtonVisual(UiButtonVisual.Framed);
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Sync(PendingEngagement pending)
	{
		_busy = false;
		_shell.Open(
			"Contact",
			$"Hostile target · {FormatFaction(pending.CounterpartyFaction)}");
		_shell.SetRequiresDecision(true);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);

		var body = HudWidgets.CreateCardList();
		if (pending.AssignedContract is { } assignedContract)
			body.AddChild(CreateContractCard(assignedContract));

		body.AddChild(CreateFleetCard(pending.EncounterIntel));

		if (pending.FleeFailsDelivery)
		{
			body.AddChild(CreateInfoCard(
				title: "Flee consequence",
				description: "Fleeing from this interceptor will immediately fail the delivery contract.",
				visual: UiCardVisual.Technical,
				minHeight: 148f));
		}

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction(
				pending.FleeFailsDelivery ? "Abandon delivery and flee" : "Flee",
				HudActionKind.Destructive,
				RequestFlee),
			new HudAction("Engage", HudActionKind.Primary, RequestEngage),
		]);
	}

	public void Close() => _shell.Close();

	public void SetBusy(bool busy) => _busy = busy;

	public bool TryHandleInput(InputEvent @event)
	{
		if (!IsOpen || _busy)
			return IsOpen;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			RequestFlee();
			return true;
		}

		return false;
	}

	private void RequestEngage()
	{
		if (_busy)
			return;

		EngageRequested?.Invoke();
	}

	private void RequestFlee()
	{
		if (_busy)
			return;

		FleeRequested?.Invoke();
	}

	private static string FormatFaction(EFaction faction) =>
		faction switch
		{
			EFaction.Pirates => "Pirates",
			EFaction.TheOptimality => "The Optimality",
			_ => faction.ToString(),
		};

	private static UiCard CreateInfoCard(
		string title,
		string subtitle = "",
		string description = "",
		string trailing = "",
		UiCardVisual visual = UiCardVisual.Angled,
		float minHeight = 0f)
	{
		var card = new UiCard(title, subtitle, description, trailing)
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(580f, minHeight),
		};
		card.SetVisual(visual);
		card.SetInteractive(false);
		return card;
	}

	private static UiCard CreateFleetCard(string encounterIntel)
	{
		return CreateInfoCard(
			title: "Contact Intel",
			description: encounterIntel,
			visual: UiCardVisual.Angled,
			minHeight: 140f);
	}

	private static UiCard CreateContractCard(Contract contract)
	{
		var subtitle = ContractDisplay.Kind(contract) == EContractKind.Hunt
			? "Active bounty"
			: "Linked contract";
		var description = ContractDisplay.Narrative(contract);

		var card = CreateInfoCard(
			ContractDisplay.Title(contract),
			subtitle,
			description,
			ContractDisplay.DifficultyStars(contract),
			UiCardVisual.Angled,
			minHeight: 200f);
		card.SetTrailingColor(HudPalettes.Gold.Primary);
		return card;
	}
}
