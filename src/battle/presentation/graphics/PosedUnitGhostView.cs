using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class PosedUnitGhostView : Node3D
{
	private const string GhostId = "posed-unit-ghost";

	private UnitView? _view;
	private EType? _type;
	private Color? _tint;

	public void Apply(PosedUnitGhostSpec? spec)
	{
		if (spec is null)
		{
			if (_view is not null)
				_view.HideVisual();
			Visible = false;
			return;
		}

		var needsBinding = _view is null || _type != spec.Type || _tint != spec.Tint;
		if (needsBinding)
		{
			_view?.QueueFree();
			_view = new UnitView { Name = "PosedUnitGhost" };
			AddChild(_view);
			_type = spec.Type;
			_tint = spec.Tint;
		}

		var chassis = ShipCatalog.SpecFor(spec.Type);
		var loadout = ShipCatalog.NewRunLoadoutFor(spec.Type);
		var stats = Stats.ForChassis(chassis);
		var state = new State
		{
			Id = GhostId,
			Type = spec.Type,
			Loadout = loadout.DeepCopy(),
			Position = spec.Position,
			Fore = spec.Fore,
			Dorsal = spec.Dorsal,
			Starboard = GrimSpace.Math.Grid.Coord.Cross(spec.Dorsal, spec.Fore),
			Stats = stats,
			HullPoints = loadout.MaxHullPoints,
			ShieldPoints = loadout.MaxShieldPoints.Clone(),
		};
		var view = _view
			?? throw new InvalidOperationException("Posed ghost view was not initialized.");
		if (needsBinding)
			view.Bind(state, spec.Tint);
		view.Present(state, UnitVisualState.Ghost);
		Visible = true;
	}
}
