using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class WreckHudPreview : Control
{
	[Export]
	public bool Ambush { get; set; }

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Wreck decision HUD preview",
			Ambush ? "Ambush briefing variant" : "Salvage briefing variant");

		var contract = CreatePreviewContract(Ambush);
		var overlay = new WreckHudOverlay();
		UiHudPreviewChrome.MountOverlay(this, overlay);
		overlay.Sync(new PendingWreckDecision(contract.Id, contract, Ambush));
	}

	private static Contract CreatePreviewContract(bool ambush)
	{
		var center = new Coord(0, 0, 0);
		var searchArea = new AreaPick(
			new AreaIntel("Somewhere in the debris field.", "poi-a", "poi-b", "poi-c"),
			[center]);
		WreckageOutcome outcome = ambush
			? new WreckageOutcome.Ambush(
				new FleetSpawnSpec(
					GrimSpace.World.StarSystem.Units.EType.PirateFleet,
					EFaction.Pirates,
					9,
					[(GrimSpace.Units.Enums.EType.RepurposedMiner, GrimSpace.Units.Enums.EShipGearTier.T0)]))
			: new WreckageOutcome.Salvage(ResourceBundle.Empty);
		return new Contract(
			"preview-wreck-contract",
			new WreckageObjective("preview-wreck-contract.wreckage", searchArea, outcome),
			EDangerLevel.VeryLow,
			EFaction.Player,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 75)),
			new ContractNarrative(
				"Derelict courier",
				ambush
					? "Scanners show a stripped hull drifting inside the debris field. Multiple hostile transponders are masked behind the wreck signature."
					: "A lightly damaged courier rests in the debris field. Salvage teams estimate recoverable cargo and intact components."));
	}
}
