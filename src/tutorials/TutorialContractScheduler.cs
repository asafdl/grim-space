using BattleUnitType = GrimSpace.Units.Enums.EType;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.Tutorials;

public sealed class TutorialContractScheduler : IDisposable
{
	internal const int BeatAHuntRewardCredits = 100;

	private readonly StarSystemOrchestrator _orchestrator;
	private bool _beatACompleted;

	public TutorialContractScheduler(StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
	}

	public void Start()
	{
		if (_beatACompleted)
			return;

		OfferBeatA(_orchestrator.Map);
		_beatACompleted = true;
		RegisterFutureBeats();
	}

	public static void OfferBeatA(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		if (map.ContractRegistry.Offered.Any(
				contract => contract.IsStoryObjective && contract.Objective is HuntObjective))
			return;

		var args = CreateBeatAHuntArgs(map);
		ContractFactory.Create(map, EContractKind.Hunt, args);
	}

	internal static HuntCreateArgs CreateBeatAHuntArgs(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		var landmarkGroups = new[]
		{
			new[] { plan.RefineryPoiId, plan.StoragePoiId },
			new[] { plan.ExtractionPoiId, plan.StoragePoiId },
			new[] { plan.RefineryPoiId, plan.ExitPoiId },
		};
		var distances = new[] { EAreaDistance.Low, EAreaDistance.Med, EAreaDistance.High };

		return new HuntCreateArgs(
			plan.AdministrativePoiId,
			new AreaPickerArgs(landmarkGroups, distances),
			new HuntEncounterArgs(
				FleetType.PirateFleet,
				EFaction.Pirates,
				EDangerLevel.VeryLow,
				[
					BattleUnitType.Patrol,
					BattleUnitType.Patrol,
					BattleUnitType.Patrol,
				]),
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatAHuntRewardCredits)),
			ContractNarrative.ForHunt("Pirate Hunt"),
			IsStoryObjective: true);
	}

	private void RegisterFutureBeats()
	{
		// TODO: subscribe on contract completion and offer Delivery beat B.
	}

	public void Dispose()
	{
	}
}
