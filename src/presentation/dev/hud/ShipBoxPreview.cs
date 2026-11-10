using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Presentation.Ui;
using EShipGearTier = GrimSpace.Units.Enums.EShipGearTier;

namespace GrimSpace.Presentation.Dev.Hud;

/// <summary>Ship boxes at picker size and large size across tiers and damage states.</summary>
public sealed partial class ShipBoxPreview : Control
{
	private static readonly (float Hull, float Shield)[] DamageStates =
		[(1f, 1f), (0.6f, 0.3f), (0.2f, 0f), (0f, 0f)];

	private UiHudDevContext? _dev;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Ship box preview",
			"Rows: T0 / T1 / T2. Columns: full, damaged, critical, empty. Then an empty slot.");

		var dev = UiHudDevContext.Create();
		_dev = dev;
		var chassis = dev.Run.ItemInventory.Get(dev.Run.GetFieldedShipIds()[0]).Spec.Chassis;
		var root = new HBoxContainer { Position = new Vector2(32f, 96f) };
		root.AddThemeConstantOverride("separation", 64);
		AddChild(root);
		foreach (var size in new[] { 48f, 96f })
		{
			var grid = new GridContainer { Columns = DamageStates.Length + 1 };
			grid.AddThemeConstantOverride("h_separation", 4);
			grid.AddThemeConstantOverride("v_separation", 4);
			root.AddChild(grid);
			foreach (var tier in new[] { EShipGearTier.T0, EShipGearTier.T1, EShipGearTier.T2 })
			{
				foreach (var (hull, shield) in DamageStates)
					grid.AddChild(new ShipBox(Damaged(dev, chassis, tier, hull, shield), size));
				grid.AddChild(new ShipBox(null, size));
			}
		}
	}

	private static ShipInstance Damaged(
		UiHudDevContext dev,
		GrimSpace.Units.Enums.EType chassis,
		EShipGearTier tier,
		float hull,
		float shield)
	{
		var id = $"preview-{tier}-{hull}-{shield}";
		dev.Run.ItemInventory.Register(
			new ShipSpawnDeclaration(id, chassis, tier, RollSeed: 7),
			GrimSpace.Run.State.PlayerFleetUnitId);
		var ship = dev.Run.ItemInventory.Get(id);
		ship.CurrentHullPoints = Mathf.RoundToInt(ship.Loadout.BaseHullPoints * hull);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			ship.CurrentShieldPoints[face] = Mathf.RoundToInt(ship.Loadout.BaseShieldPoints[face] * shield);
		return ship;
	}

	public override void _ExitTree() => _dev?.Run.Dispose();
}
