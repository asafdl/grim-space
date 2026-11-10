using Godot;
using GrimSpace.Run;
using GrimSpace.Units;
using EShipGearTier = GrimSpace.Units.Enums.EShipGearTier;
using EOperatorClassId = GrimSpace.Units.Enums.EOperatorClassId;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class EngagementHudPreview : Control
{
	private UiHudDevContext? _dev;
	private EngagementHudOverlay? _overlay;
	private readonly DeploymentSlots _deployment = new();

	[Export]
	public bool IncludeAssignedContract { get; set; } = true;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(
			this,
			"Engagement HUD preview",
			"Drag pilots into slots; drag back to the roster to remove. Dev-only selection.");

		var dev = UiHudDevContext.Create();
		_dev = dev;
		while (dev.Run.PilotRegistry.All.Count < 8)
		{
			var classId = new[]
			{
				EOperatorClassId.SpaceRanger, EOperatorClassId.Support,
				EOperatorClassId.Tank, EOperatorClassId.Rogue,
			}[(dev.Run.PilotRegistry.All.Count - 2) % 4];
			var pilot = Pilot.CreateForShip($"preview-unassigned-{dev.Run.PilotRegistry.All.Count}", classId);
			pilot = PilotProgression.AddExperience(pilot, PilotProgression.ExperienceForLevel(2));
			dev.Run.PilotRegistry.Register(pilot);
			dev.Run.PilotRegistry.Unassign(pilot.Id);
		}
		var fielded = dev.Run.PilotRegistry.All.First(pilot => pilot.AssignedShipId is not null);
		var fieldedChassis = dev.Run.ItemInventory.Get(fielded.AssignedShipId!).Spec.Chassis;
		foreach (var tier in new[] { EShipGearTier.T0, EShipGearTier.T1, EShipGearTier.T2 })
			dev.Run.ItemInventory.Register(
				new ShipSpawnDeclaration($"preview-spare-{tier}", fieldedChassis, tier, RollSeed: 7),
				GrimSpace.Run.State.PlayerFleetUnitId);

		var roster = new PlayerFleetHud { Position = new Vector2(16f, 72f), Deployment = _deployment };
		AddChild(roster);
		roster.Sync(dev.Run);
		var overlay = new EngagementHudOverlay();
		_overlay = overlay;
		UiHudPreviewChrome.MountOverlay(this, overlay);
		_deployment.Changed += () =>
		{
			roster.Sync(dev.Run);
			SyncOverlay();
		};
		_deployment.Reset(dev.Run);
		MoveChild(roster, GetChildCount() - 1);
	}

	private void SyncOverlay()
	{
		var dev = _dev!;
		var deployable = _deployment.DeployedPilotIds.Count;
		_overlay!.Sync(new PendingEngagement(
			"preview-pirate-fleet",
			EType.PirateFleet,
			EFaction.Pirates,
			"2 × Repurposed Miner (T1)\n1 × Fighter (T0)",
			IncludeAssignedContract ? dev.SampleHuntContract() : null,
			FleeFailsDelivery: false,
			HazardPayCredits: StrategicEconomyConfig.HazardPayCreditsPerPilot * deployable,
			FleeFuelCost: StrategicEconomyConfig.FleeFuelForLevel(1) * 2,
			AvailableFuel: StrategicEconomyConfig.StartingFuel,
			DeployablePilots: deployable), _deployment);
	}

	public override void _ExitTree() => _dev?.Run.Dispose();
}
