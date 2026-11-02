using Godot;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

/// <summary>Black-market sale with pilot hiring and ship purchases in one modal.</summary>
public sealed partial class BlackMarketSaleHudPreview : Control
{
	public override void _Ready()
	{
		var dev = UiHudDevContext.Create();
		var setup = dev.PrepareBlackMarketSalePreview();
		var pilotContractId = setup.Pilots.ContractId;

		UiHudPreviewChrome.Attach(
			this,
			"Black market sale HUD preview",
			"Pilots and ships share one credit pool");

		var hud = new BlackMarketSaleHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, hud);
		hud.Open(dev.Run, "", pilotContractId);

		void Sync()
		{
			dev.StarSystem.RefreshPlayerAgent();
			hud.Sync(dev.Run, pilotContractId);
		}

		hud.HireRequested += slot =>
		{
			if (!dev.Intents.TryHirePilot(setup.Pilots.Endpoint, slot))
				GD.PushWarning($"Pilot hire rejected for slot {slot}.");
			Sync();
		};
		hud.RecruitmentRequested += offer =>
		{
			if (!dev.Intents.TryPurchaseShip(setup.ShipsEndpoint, offer))
				GD.PushWarning($"Ship purchase rejected for {offer.Chassis} {offer.GearTier}.");
			Sync();
		};
	}
}
