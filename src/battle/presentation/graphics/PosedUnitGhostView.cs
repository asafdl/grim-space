using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Components;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class PosedUnitGhostView : Node3D
{
	private const string GhostId = "posed-unit-ghost";
	private const string ReticleTexturePath =
		"res://assets/ui/components/runtime/reticle_hollow_neutral.png";
	private const float ReticleTurnSpeed = 0.35f;

	private UnitView? _view;
	private EType? _type;
	private Color? _tint;
	private Sprite3D? _reticle;
	private float _reticleAngle;

	public override void _Process(double delta)
	{
		if (_reticle is null)
			return;

		_reticleAngle = Mathf.Wrap(
			_reticleAngle + (float)delta * ReticleTurnSpeed,
			-Mathf.Pi,
			Mathf.Pi);
		_reticle.Rotation = new Vector3(0f, 0f, _reticleAngle);
	}

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
			_reticle = CreateReticle();
			_view.AddChild(_reticle);
		}

		var chassis = ShipCatalog.SpecFor(spec.Type);
		var loadout = ShipCatalog.NewRunLoadoutFor(spec.Type);
		var state = new State
		{
			Id = GhostId,
			Type = spec.Type,
			Initiative = chassis.Initiative,
			Loadout = loadout.DeepCopy(),
			Position = spec.Position,
			Fore = spec.Fore,
			Dorsal = spec.Dorsal,
			Starboard = GrimSpace.Math.Grid.Coord.Cross(spec.Dorsal, spec.Fore),
			Maneuverability = chassis.Maneuverability,
			HullPoints = loadout.MaxHullPoints,
			ShieldPoints = loadout.MaxShieldPoints.Clone(),
		};
		var view = _view
			?? throw new InvalidOperationException("Posed ghost view was not initialized.");
		if (needsBinding)
			view.Bind(state, spec.Tint);
		view.Present(state, UnitVisualState.SelectedGhost);
		Visible = true;
	}

	private static Sprite3D CreateReticle()
	{
		return new Sprite3D
		{
			Name = "TutorialReticleSprite",
			Texture = GD.Load<Texture2D>(ReticleTexturePath)
				?? throw new InvalidOperationException(
					$"Could not load reticle texture '{ReticleTexturePath}'."),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			Shaded = false,
			PixelSize = 0.0035f,
			Scale = Vector3.One * 2.21f,
			Modulate = HudPalettes.PurpleRed.Primary,
		};
	}
}
