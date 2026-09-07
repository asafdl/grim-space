using Godot;
using GrimSpace.Presentation.Ui.Hud;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class EngagementHudOverlay : Node
{
	private readonly ModalHudShell _shell;
	private string _targetUnitId = "";

	public event Action? Dismissed;

	public EngagementHudOverlay()
	{
		_shell = new ModalHudShell();
		AddChild(_shell);
		_shell.Closed += () => Dismissed?.Invoke();
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open(StarMap map, string targetUnitId)
	{
		if (!map.UnitRegistry.TryGet(targetUnitId, out var target))
			return;

		_targetUnitId = targetUnitId;
		var profile = target.State.CombatProfile
			?? throw new InvalidOperationException($"Engagement target '{targetUnitId}' has no combat profile.");

		_shell.Open("Contact", "Hostile fleet detected");
		_shell.SetHeader(HudHeaderMode.Close, Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);

		var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", HudStyles.HalfMargin);
		body.AddChild(HudWidgets.CreateSection("Fleet type", target.State.Type.ToString()));
		body.AddChild(HudWidgets.CreateSection("Faction", FormatFaction(target.State.Faction)));
		body.AddChild(HudWidgets.CreateSection(
			"Threat",
			profile.Danger.ToString(),
			bodyRole: HudTextRole.Danger));

		_shell.SetBody(body);
	}

	public void Close()
	{
		_targetUnitId = "";
		_shell.Close();
	}

	public bool TryHandleInput(InputEvent @event)
	{
		if (!IsOpen)
			return false;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }
			|| @event is InputEventMouseButton { Pressed: true })
		{
			Close();
			return true;
		}

		return false;
	}

	private static string FormatFaction(EFaction faction) =>
		faction switch
		{
			EFaction.Pirates => "Pirates",
			EFaction.TheOptimality => "The Optimality",
			_ => faction.ToString(),
		};
}
