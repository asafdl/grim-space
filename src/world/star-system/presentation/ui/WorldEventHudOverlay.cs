using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public interface IWorldEventHud
{
	event Action? DismissRequested;

	bool IsOpen { get; }

	void Sync(WorldEventPresentation presentation, ResourceBundle? reward = null);

	void Close();

	void SetBusy(bool busy);

	bool TryHandleInput(InputEvent @event);
}

public sealed partial class WorldEventHudOverlay : Control, IWorldEventHud
{
	private readonly ModalShell _shell;
	private bool _busy;

	public event Action? DismissRequested;

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

	public void Sync(WorldEventPresentation presentation, ResourceBundle? reward = null)
	{
		_busy = false;
		_shell.Open(presentation.Title);
		_shell.SetRequiresDecision(true);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);

		var body = HudWidgets.CreateCardList();
		body.AddChild(CreateBodyCard(presentation.Body, reward));
		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction("Dismiss", HudActionKind.Primary, RequestDismiss),
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
}
