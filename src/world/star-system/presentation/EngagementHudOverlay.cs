using Godot;
using GrimSpace.Components;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class EngagementHudOverlay : Node
{
	private readonly ModalShell _shell;
	private bool _busy;

	public event Action? EngageRequested;
	public event Action? FleeRequested;

	public EngagementHudOverlay()
	{
		_shell = new ModalShell();
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Sync(PendingEngagement pending)
	{
		_busy = false;
		_shell.Open("Contact", "Hostile fleet detected");
		_shell.SetHeader(HudHeaderMode.Close, null);
		_shell.SetHeaderVisible(false);
		_shell.SetBackHandler(null);
		_shell.SetCloseHandler(null);

		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		body.AddChild(HudWidgets.CreateSection("Fleet type", pending.CounterpartyType.ToString()));
		body.AddChild(HudWidgets.CreateSection("Faction", FormatFaction(pending.CounterpartyFaction)));
		body.AddChild(HudWidgets.CreateSection(
			"Threat",
			pending.Danger.ToString(),
			bodyRole: HudTextRole.Danger));

		_shell.SetBody(body);
		_shell.SetFooter(
		[
			new HudAction("Flee", HudActionKind.Secondary, RequestFlee),
			new HudAction("Engage", HudActionKind.Primary, RequestEngage),
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
}
