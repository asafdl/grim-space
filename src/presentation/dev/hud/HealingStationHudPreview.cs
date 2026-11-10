using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class HealingStationHudPreview : Control
{
	private static readonly (EShipGearTier Tier, float Hull, float Shield)[] HoldShips =
		[
			(EShipGearTier.T2, 0.4f, 0f), (EShipGearTier.T1, 1f, 0.5f), (EShipGearTier.T0, 1f, 1f),
			(EShipGearTier.T1, 0.1f, 0.2f), (EShipGearTier.T0, 0.5f, 1f), (EShipGearTier.T2, 1f, 1f),
			(EShipGearTier.T0, 0.8f, 0.6f), (EShipGearTier.T1, 0.3f, 0f), (EShipGearTier.T0, 1f, 0.9f),
			(EShipGearTier.T2, 0.7f, 0.4f),
		];

	private UiHudDevContext? _dev;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Healing station HUD preview",
			"Right-click Heal selected to pick a ship; 11 owned ships with mixed damage");

		var dev = UiHudDevContext.Create();
		_dev = dev;
		var fielded = dev.Run.ItemInventory.Get(dev.Run.GetFieldedShipIds()[0]);
		Damage(fielded, 0.7f, 0.25f);
		foreach (var (tier, hull, shield) in HoldShips)
		{
			var id = $"preview-hold-{tier}-{hull}-{shield}";
			dev.Run.ItemInventory.Register(
				new ShipSpawnDeclaration(id, fielded.Spec.Chassis, tier, RollSeed: 7),
				GrimSpace.Run.State.PlayerFleetUnitId);
			Damage(dev.Run.ItemInventory.Get(id), hull, shield);
		}

		var endpoint = dev.TradeHubEndpoint(
			TradeHub.DockyardFacilitySlug,
			TradeHub.DockyardHealingStationSceneSlotId);
		var overlay = new CatalogMerchantHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Open(
			dev.Run,
			dev.Intents,
			endpoint,
			EMerchantCatalog.Healing,
			"Healing station",
			"Preview healing bay");
		foreach (var ship in dev.Run.ItemInventory.ShipsOwnedBy(GrimSpace.Run.State.PlayerFleetUnitId))
			overlay.ReceiveCatalogQuote(dev.BuildCatalogQuote(endpoint, EMerchantCatalog.Healing, ship));
	}

	private static void Damage(ShipInstance ship, float hull, float shield)
	{
		ship.CurrentHullPoints = Mathf.RoundToInt(ship.Loadout.BaseHullPoints * hull);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			ship.CurrentShieldPoints[face] = Mathf.RoundToInt(ship.Loadout.BaseShieldPoints[face] * shield);
	}

	public override void _ExitTree() => _dev?.Run.Dispose();
}
