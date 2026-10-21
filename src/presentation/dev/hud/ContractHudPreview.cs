using Godot;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class ContractHudPreview : Control
{
	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Contract HUD preview",
			"F6 scene · interact with list and detail flows");

		var dev = UiHudDevContext.Create();
		var overlay = new ContractHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);

		if (!dev.TryFindContractBoardVisit(out var visit))
		{
			GD.PushError("No contract board operator with pending contracts in preview seed.");
			return;
		}

		overlay.Open(
			dev.Map,
			visit.PoiId,
			visit.FacilityId,
			visit.OperatorName);
	}
}
