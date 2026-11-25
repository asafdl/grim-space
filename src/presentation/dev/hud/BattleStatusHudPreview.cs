using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class BattleStatusHudPreview : Control
{
	private BattleCommandHud _hud = null!;
	private State _player = null!;
	private State _enemy = null!;
	private bool _inspecting;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Battle status HUD preview",
			"Hover icons for effects. Space: inspect enemy / return. R: add a Reactive Shields stack.");
		_hud = new BattleCommandHud
		{
			AnchorLeft = 0.5f,
			AnchorTop = 1f,
			AnchorRight = 0.5f,
			AnchorBottom = 1f,
			GrowHorizontal = GrowDirection.Both,
			GrowVertical = GrowDirection.Begin,
			Theme = HudThemes.Load(HudThemeFamily.Battle),
		};
		AddChild(_hud);
		_hud.ReturnToPlayerRequested += () =>
		{
			_inspecting = false;
			Refresh();
		};
		_player = State.FromShipInstance(ShipInstance.FromCatalog("preview_player", EType.Fighter), Coord.Zero);
		EPilotCapabilityId[] passives =
		[
			EPilotCapabilityId.ScrapDroneSwarmDamageBonus,
			EPilotCapabilityId.HealingSkillRangeBonus,
			EPilotCapabilityId.LightningShieldDamageBonus,
			EPilotCapabilityId.AdjacentDamageBonus,
			EPilotCapabilityId.ReinforcedHull,
			EPilotCapabilityId.InTheFray,
			EPilotCapabilityId.ReactiveShields,
			EPilotCapabilityId.StationaryDamageDeflection,
			EPilotCapabilityId.ShieldDynamo,
			EPilotCapabilityId.OneWithTheVoid,
		];
		foreach (var capability in passives)
			_player.Statuses.Add(BattleStatusMaterializer.Create(capability));
		_player.StatusOf<ReactiveShields>()!.Stacks = 3;
		_player.Overdrive = true;
		_player.Statuses.Add(new ProtectionDroneBuff());
		_player.Statuses.Add(ShieldBrace.CreateStatus(
			_player.Id, ESpatialOrientation.Forward, new FaceShieldPoints(), 42));
		TemporaryShields.GrantPerFace(_player, "first", 10);
		TemporaryShields.GrantPerFace(_player, "second", 20);
		_enemy = State.FromShipInstance(ShipInstance.FromCatalog("preview_enemy", EType.Fighter), Coord.Zero);
		_enemy.Overheat = true;
		Refresh();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		switch (key.Keycode)
		{
			case Key.Space:
				_inspecting = !_inspecting;
				break;
			case Key.R:
				_player.StatusOf<ReactiveShields>()!.Stacks++;
				break;
			default:
				return;
		}
		Refresh();
		GetViewport().SetInputAsHandled();
	}

	private void Refresh()
	{
		var state = _inspecting ? _enemy : _player;
		var unit = UnitDisplayState.Capture(state, new ActorRuntime());
		_hud.ApplyPilotPortrait(_inspecting ? null : "robot_v1:42");
		_hud.ApplyResources(unit.CurrentActionPoints, unit.BaseActionPoints, unit.CurrentManeuverPoints, unit.BaseManeuverPoints, unit);
		_hud.ApplyDefenses(unit.CurrentHullPoints, unit.BaseHullPoints, unit.CurrentShieldPoints, unit.Loadout.BaseShieldPoints);
		_hud.ApplyMainActions(EPlayerMode.Move, false, unit, AbilityLegality.Empty);
		_hud.ApplyCommands(_inspecting, false, false, false, false, false);
		_hud.StatusBar.Apply(unit);
	}
}
