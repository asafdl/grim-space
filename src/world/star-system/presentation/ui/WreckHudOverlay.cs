using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class WreckHudOverlay : Control
{
	private readonly ModalShell _shell;
	private bool _busy;
	private bool _isAmbush;

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

		_shell = new ModalShell();
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Sync(PendingWreckDecision pending)
	{
		_busy = false;
		_isAmbush = pending.IsAmbush;
		_shell.Open("Wreckage", _isAmbush ? "Hostile fleets spotted" : pending.Title);
		_shell.SetHeader(HudHeaderMode.Close, null);
		_shell.SetHeaderVisible(false);
		_shell.SetBackHandler(null);
		_shell.SetCloseHandler(null);

		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		if (_isAmbush)
			body.AddChild(HudWidgets.CreateSection("Contract", pending.Title));
		body.AddChild(HudWidgets.CreateSection("Briefing", pending.Briefing));
		if (_isAmbush)
			body.AddChild(HudWidgets.CreateSection(
				"Warning",
				"Hostile fleets have been spotted near the wreck. Investigating could lead us into an ambush. Fleeing will fail this contract.",
				bodyRole: HudTextRole.Danger));

		_shell.SetBody(body);
		IReadOnlyList<HudAction> actions = _isAmbush
			? [
				new HudAction("Flee", HudActionKind.Destructive, RequestLeave),
				new HudAction("Continue to wreck", HudActionKind.Primary, RequestInvestigate),
			]
			: [
				new HudAction("Leave", HudActionKind.Secondary, RequestLeave),
				new HudAction("Investigate", HudActionKind.Primary, RequestInvestigate),
			];
		_shell.SetFooter(actions);
	}

	public void Close() => _shell.Close();

	public void SetBusy(bool busy) => _busy = busy;

	public void ShowError(string message)
	{
		_busy = false;
		_shell.SetSubtitle(message);
	}

	public bool TryHandleInput(InputEvent @event)
	{
		if (!IsOpen || _busy)
			return IsOpen;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			if (!_isAmbush)
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
}
