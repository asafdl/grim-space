using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class WreckHudOverlay : Control
{
	private readonly ModalShell _shell;
	private bool _busy;

	public event Action? InvestigateRequested;
	public event Action? LeaveRequested;

	public WreckHudOverlay()
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

	public void Sync(PendingWreckDecision pending)
	{
		_busy = false;
		var contract = pending.Contract;
		_shell.Open("Wreckage");
		_shell.SetHeader(HudHeaderMode.Close, null);
		_shell.SetHeaderVisible(true);
		_shell.SetDismissVisible(true);
		_shell.SetBackHandler(null);
		_shell.SetCloseHandler(RequestLeave);

		var body = HudWidgets.CreateCardList();
		body.AddChild(CreateContractCard(contract));

		if (pending.IsAmbush)
		{
			body.AddChild(CreateInfoCard(
				title: "Hostile Signatures Detected!",
				description: "This is likely an ambush, but fleeing won't get us paid.",
				visual: UiCardVisual.Angled,
				palette: HudPalettes.PurpleRed,
				minHeight: 148f));
		}

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction(
				pending.IsAmbush ? "Flee" : "Leave",
				HudActionKind.Destructive,
				RequestLeave),
			new HudAction(
				pending.IsAmbush ? "Continue" : "Investigate",
				HudActionKind.Primary,
				RequestInvestigate),
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
			RequestLeave();
			return true;
		}

		return false;
	}

	private void RequestInvestigate()
	{
		if (_busy)
			return;

		InvestigateRequested?.Invoke();
	}

	private void RequestLeave()
	{
		if (_busy)
			return;

		LeaveRequested?.Invoke();
	}

	private static UiCard CreateInfoCard(
		string title,
		string subtitle = "",
		string description = "",
		string trailing = "",
		UiCardVisual visual = UiCardVisual.Angled,
		HudPalette? palette = null,
		float minHeight = 160f)
	{
		var card = new UiCard(title, subtitle, description, trailing)
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(580f, minHeight),
		};
		card.SetVisual(visual);
		if (palette is { } customPalette)
			card.SetPalette(customPalette);
		card.SetInteractive(false);
		return card;
	}

	private static UiCard CreateContractCard(Contract contract)
	{
		var card = CreateInfoCard(
			ContractDisplay.Title(contract),
			ContractDisplay.TypeDisplayName(contract),
			ContractDisplay.Narrative(contract),
			ContractDisplay.DifficultyStars(contract),
			UiCardVisual.Angled,
			minHeight: 200f);
		card.SetTrailingColor(HudPalettes.Gold.Primary);
		return card;
	}
}
