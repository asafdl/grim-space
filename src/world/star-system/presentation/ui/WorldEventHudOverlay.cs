using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Presentation.Facilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public interface IWorldEventHud
{
	event Action? DismissRequested;

	event Action? AcceptRequested;

	event Action? RejectRequested;

	bool IsOpen { get; }

	void Sync(
		WorldEventPresentation presentation,
		ResourceBundle? reward = null,
		PilotRecruitProfile? recruit = null,
		bool stakeChoice = false);

	void Close();

	void SetBusy(bool busy);

	bool TryHandleInput(InputEvent @event);
}

public sealed partial class WorldEventHudOverlay : Control, IWorldEventHud
{
	private readonly ModalShell _shell;
	private bool _busy;
	private bool _recruitChoice;
	private bool _stakeChoice;

	public event Action? DismissRequested;

	public event Action? AcceptRequested;

	public event Action? RejectRequested;

	public WorldEventHudOverlay()
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

	public void Sync(
		WorldEventPresentation presentation,
		ResourceBundle? reward = null,
		PilotRecruitProfile? recruit = null,
		bool stakeChoice = false)
	{
		_busy = false;
		_recruitChoice = recruit is not null;
		_stakeChoice = stakeChoice && recruit is null;
		_shell.Open(presentation.Title);
		_shell.SetRequiresDecision(true);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);

		var body = HudWidgets.CreateCardList();
		body.AddChild(recruit is null
			? CreateBodyCard(presentation.Body, reward)
			: CreateRecruitCard(presentation.Body, recruit));
		_shell.SetBody(body);
		_shell.SetFooter(Footer());
	}

	private HudAction[] Footer()
	{
		if (_recruitChoice)
		{
			return
			[
				new HudAction("Reject", HudActionKind.Destructive, RequestReject),
				new HudAction("Accept", HudActionKind.Primary, RequestAccept),
			];
		}

		if (_stakeChoice)
		{
			return
			[
				new HudAction(
					"Ignore request",
					HudActionKind.Destructive,
					RequestReject,
					badgeIconPath: EconomyIconCatalog.LeaveIconPath,
					badgeTooltip: new HudTooltipContent(
						"Leave",
						WorldEventCatalog.Stake.IgnoreTooltipBody)),
				new HudAction("Pay", HudActionKind.Primary, RequestAccept),
			];
		}

		return [new HudAction("Dismiss", HudActionKind.Primary, RequestDismiss)];
	}

	public void Close() => _shell.Close();

	public void SetBusy(bool busy) => _busy = busy;

	public bool TryHandleInput(InputEvent @event)
	{
		if (!IsOpen || _busy)
			return IsOpen;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			if (_recruitChoice || _stakeChoice)
				RequestReject();
			else
				RequestDismiss();
			return true;
		}

		return false;
	}

	private void RequestDismiss()
	{
		if (_busy)
			return;

		DismissRequested?.Invoke();
	}

	private void RequestAccept()
	{
		if (_busy)
			return;

		AcceptRequested?.Invoke();
	}

	private void RequestReject()
	{
		if (_busy)
			return;

		RejectRequested?.Invoke();
	}

	private static UiCard CreateBodyCard(string body, ResourceBundle? reward)
	{
		var card = new UiCard(body, "", "", "")
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeOverride = new UiSizeOverride(Height: 140f),
		};
		card.SetVisual(UiCardVisual.Technical);
		card.SetInteractive(false);
		if (reward is { IsEmpty: false })
			card.SetTrailingContent(ResourceRewardDisplay.CreateCompact(reward));
		return card;
	}

	private static UiCard CreateRecruitCard(string body, PilotRecruitProfile recruit)
	{
		var card = new UiCard(
			PilotHireDisplay.CardTitle(recruit.BackgroundId),
			PilotHireDisplay.CardSubtitle(recruit),
			body,
			"",
			ShipPilotPortraitCatalog.TextureFor(recruit.PortraitId))
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeOverride = new UiSizeOverride(Height: 200f),
		};
		card.SetVisual(UiCardVisual.Technical);
		card.SetInteractive(false);
		card.SetTrailingContent(PilotIdentityIconRail.BuildFor(recruit));
		return card;
	}
}
