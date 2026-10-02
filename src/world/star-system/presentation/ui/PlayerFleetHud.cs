using Godot;
using GrimSpace.Run;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public partial class PlayerFleetHud : MarginContainer
{
	private readonly Dictionary<string, ShipPilot> _pilots = new(StringComparer.Ordinal);
	private HBoxContainer _pilotRow = null!;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;

		var margin = new MarginContainer();
		foreach (var marginName in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
			margin.AddThemeConstantOverride(marginName, 8);
		AddChild(margin);

		_pilotRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_pilotRow.AddThemeConstantOverride("separation", 8);
		margin.AddChild(_pilotRow);
	}

	public void Sync(State run)
	{
		run.EnsurePlayerShipPortraits(run.PlayerParty.ShipIds);

		foreach (var shipId in run.PlayerParty.ShipIds)
		{
			if (!run.ShipRegistry.TryGet(shipId, out var ship)
				|| !run.PlayerShipPortraitIds.TryGetValue(shipId, out var portraitId))
				continue;

			if (!_pilots.TryGetValue(shipId, out var pilot))
			{
				pilot = new ShipPilot();
				_pilots[shipId] = pilot;
				_pilotRow.AddChild(pilot);
			}

			pilot.SetState(portraitId, ship);
		}

		foreach (var shipId in _pilots.Keys.Except(run.PlayerParty.ShipIds, StringComparer.Ordinal).ToArray())
		{
			_pilots[shipId].QueueFree();
			_pilots.Remove(shipId);
		}
	}
}
