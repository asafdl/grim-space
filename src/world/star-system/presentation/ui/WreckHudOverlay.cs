using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Contact;

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

		_shell = new ModalShell();
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Sync(PendingWreckDecision pending)
	{
		_busy = false;
		_shell.Open("Wreckage", pending.Title);
		_shell.SetHeader(HudHeaderMode.Close, null);
		_shell.SetHeaderVisible(false);
		_shell.SetBackHandler(null);
		_shell.SetCloseHandler(null);

		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		body.AddChild(HudWidgets.CreateSection("Briefing", pending.Briefing));

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction("Leave", HudActionKind.Secondary, RequestLeave),
			new HudAction("Investigate", HudActionKind.Primary, RequestInvestigate),
		]);
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
