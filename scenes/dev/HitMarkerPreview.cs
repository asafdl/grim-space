using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Dev;

public partial class HitMarkerPreview : Node3D
{
	private static readonly Color PlayerTint = new(0.38f, 0.74f, 1f);
	private static readonly EType[] ShipTypes =
	[
		EType.Fighter,
		EType.Gunship,
		EType.Carrier,
		EType.RepurposedMiner,
		EType.VoidBomb,
	];

	private const double AutoHitSeconds = 1.8;

	private UnitView? _view;
	private int _typeIndex;
	private int _damage = 12;
	private bool _shieldDamage = true;
	private bool _lightningHit;
	private bool _frozen;
	private Label _status = null!;
	private Godot.Timer _autoTimer = null!;

	public override void _Ready()
	{
		var focus = WorldMapping.ToWorld(Coord.Zero);
		var camera = GetNode<Camera3D>("Camera3D");
		camera.Position = focus + new Vector3(5.2f, 3.4f, 7.5f);
		camera.LookAt(focus, Vector3.Up);

		_status = GetNode<Label>("CanvasLayer/StatusLabel");
		_autoTimer = new Godot.Timer { WaitTime = AutoHitSeconds };
		_autoTimer.Timeout += PlayHit;
		AddChild(_autoTimer);

		RebuildShip();
		PlayHit();
		_autoTimer.Start();
		UpdateStatus();
	}

	public override void _Process(double delta)
	{
		if (_view is not null)
			_view.RotateY((float)delta * 0.18f);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Space:
				PlayHit();
				break;
			case Key.L:
				_lightningHit = !_lightningHit;
				break;
			case Key.C:
				_shieldDamage = !_shieldDamage;
				PlayHit();
				break;
			case Key.P:
				_frozen = !_frozen;
				if (_frozen)
					_autoTimer.Stop();
				else
					_autoTimer.Start();
				break;
			case Key.Up:
				_damage = System.Math.Min(_damage + 3, 99);
				break;
			case Key.Down:
				_damage = System.Math.Max(_damage - 3, 1);
				break;
			case Key.Right:
				_typeIndex = (_typeIndex + 1) % ShipTypes.Length;
				RebuildShip();
				break;
			case Key.Left:
				_typeIndex = (_typeIndex - 1 + ShipTypes.Length) % ShipTypes.Length;
				RebuildShip();
				break;
			default:
				return;
		}

		UpdateStatus();
		GetViewport().SetInputAsHandled();
	}

	private void RebuildShip()
	{
		_view?.QueueFree();
		_view = new UnitView { Name = "PreviewUnit" };
		AddChild(_view);
		var state = CreatePreviewState(ShipTypes[_typeIndex]);
		_view.Bind(state, PlayerTint);
		_view.Sync(state);
	}

	private void PlayHit()
	{
		if (_view is null)
			return;

		_view.PlayImpactSparks(
			_shieldDamage ? _damage : 0,
			_shieldDamage ? 0 : _damage,
			lightning: _lightningHit);
		_view.PlayDamagePopup(
			_shieldDamage ? _damage : 0,
			_shieldDamage ? 0 : _damage);
		_autoTimer.Start();
		UpdateStatus();
	}

	private static State CreatePreviewState(EType type)
	{
		var chassis = ShipCatalog.SpecFor(type);
		var loadout = ShipCatalog.NewRunLoadoutFor(type);
		return new State
		{
			Id = "hit-preview",
			Type = type,
			Initiative = chassis.Initiative,
			Loadout = loadout.DeepCopy(),
			Position = Coord.Zero,
			Fore = Coord.Forward,
			Dorsal = Coord.Up,
			Starboard = Coord.Cross(Coord.Up, Coord.Forward),
			Maneuverability = chassis.Maneuverability,
			HullPoints = loadout.MaxHullPoints,
			ShieldPoints = loadout.MaxShieldPoints.Clone(),
		};
	}

	private void UpdateStatus()
	{
		var type = ShipTypes[_typeIndex];
		_status.Text =
			$"Hit feedback preview  |  {type}  |  Damage: {_damage}  |  Layer: {(_shieldDamage ? "Shield" : "Hull")}  |  {(_lightningHit ? "Lightning" : "Sparks")}  |  {(_frozen ? "Frozen" : "Looping")}\n" +
			"Space: hit   C: shield/hull layer   L: lightning/sparks   P: freeze loop   Up/Down: damage   Left/Right: ship type";
	}
}
