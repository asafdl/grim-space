using Godot;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Dev;

public partial class TargetDamagePreview : Node3D
{
	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.012f, 0.02f, 0.035f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.65f, 0.72f, 0.85f),
				AmbientLightEnergy = 0.8f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		var camera = new Camera3D
		{
			Position = new Vector3(11, 12, 27),
			Current = true,
			Fov = 40,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(11, 4, 4));

		var sourceState = State.FromShipInstance(
			ShipInstance.FromCatalog("attacker", EType.Fighter), new Coord(5, 2, 8));
		var source = new Node3D { Position = WorldMapping.ToWorld(sourceState.Position) };
		AddChild(source);
		var sourceDisplay = UnitDisplayState.Capture(sourceState, new ActorRuntime());
		var units = new Dictionary<string, UnitDisplayState>();
		var skulls = new List<PoseHitOpportunity>();
		for (var i = 0; i < 2; i++)
		{
			var state = State.FromShipInstance(
				ShipInstance.FromCatalog($"target-{i}", EType.Fighter), new Coord(3 + i * 5, 2, 2));
			var unit = UnitDisplayState.Capture(state, new ActorRuntime());
			units.Add(unit.Id, unit);
			var view = new UnitView();
			AddChild(view);
			view.Bind(state, new Color(1f, 0.5f, 0.42f));
			var face = unit.ToBodyFrame().HitFaceFrom(sourceState.Position);
			var impact = new ImpactFacts(
				sourceState.Id, state.Id, EImpactCause.LightningCannonBurst, face,
				state.CurrentShieldPoints[face],
				i == 0 ? state.CurrentHullPoints / 3 : state.CurrentHullPoints,
				state.CurrentShieldPoints[face], state.CurrentHullPoints);
			var shieldDamage = new FaceShieldPoints { [face] = impact.ShieldDamage };
			var shieldBefore = new FaceShieldPoints { [face] = impact.ShieldBefore };
			var tether = new EnemyRangeTetherView();
			tether.Configure(new GrimSpace.Math.Grid.Grid(20, 20, 20));
			AddChild(tether);
			tether.Apply(
				source, view,
				BattleDistance.SeparationMeters(sourceState.Position, state.Position),
				sourceDisplay, unit,
				damage: new AbilityDamagePreview(
					impact.HullDamage, shieldDamage, impact.HullBefore, shieldBefore, impact.IsLethal));
			if (impact.IsLethal)
				skulls.Add(new PoseHitOpportunity(
					unit.Id, TargetOpportunityOverlay.LethalIconPath, TargetOpportunityOverlay.LethalIconTint));
		}

		var layer = new CanvasLayer();
		AddChild(layer);
		var badges = new TargetOpportunityOverlay();
		badges.Configure(camera);
		layer.AddChild(badges);
		badges.Apply(units, skulls);
		var label = new Label
		{
			Position = new Vector2(24, 24),
			Text = "Targeting reticle damage preview\n"
				+ "Left: shields depleted + partial hull loss | Right: lethal hit + skull\n"
				+ "White pulse + glow: predicted loss | Blue/red: remaining shields/hull\n"
				+ "Off-white skull: lethal hit | Preview fixtures; no attack is committed.",
		};
		label.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(label);
	}
}
