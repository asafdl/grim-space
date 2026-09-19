using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Objectives;

public sealed class ContractObjectiveProjectionTests(StarMapFixture maps)
{
	[Fact]
	public void Project_StarterContract_UsesPersistedIntelTitleAndReward()
	{
		var map = maps.Fresh(42);
		var contract = map.ContractRegistry.Offered.Single();
		var objective = ContractObjectiveProjection.Project(map, contract);
		var hunt = (HuntObjective)contract.Objective;
		var searchArea = hunt.SpawnGroups[0].SearchArea;
		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(searchArea.Relation);
		var landmarkA = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkAId);
		var landmarkB = map.PointsOfInterest.First(poi => poi.Id == relation.LandmarkBId);
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(searchArea.Intel, out var segments));

		Assert.Equal("Pirate Hunt ★", objective.Title);
		var route = Assert.IsType<ObjectiveSummaryContent.RouteBetweenLandmarks>(objective.Summary);
		Assert.Equal(segments.Prefix, route.Prefix);
		Assert.Equal(relation.LandmarkAId, route.LandmarkAPoiId);
		Assert.Equal(landmarkA.DisplayName, route.LandmarkADisplayName);
		Assert.Equal(segments.Connector, route.Connector);
		Assert.Equal(relation.LandmarkBId, route.LandmarkBPoiId);
		Assert.Equal(landmarkB.DisplayName, route.LandmarkBDisplayName);
		Assert.Equal(segments.Suffix, route.Suffix);
		Assert.Equal(
			ContractDisplay.SearchArea(contract, map),
			AreaIntelDisplay.FormatPlain(
				searchArea.Intel,
				poiId => map.PointsOfInterest.FirstOrDefault(poi => poi.Id == poiId)?.DisplayName));
		Assert.True(objective.Reward.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(StarMap.StarterContractRewardCredits, credits);
	}

	[Fact]
	public void Project_NonLinkableIntel_FallsBackToPlainPreview()
	{
		var map = maps.Fresh(42);
		var contract = CreateContract(
			map,
			new AreaIntel("No landmarks here.", "poi-a", "poi-b"),
			null!);

		var objective = ContractObjectiveProjection.Project(map, contract);

		var plain = Assert.IsType<ObjectiveSummaryContent.Plain>(objective.Summary);
		Assert.Equal(ContractDisplay.ObjectivePreview(contract), plain.Text);
	}

	[Fact]
	public void Project_MissingLandmarkId_FallsBackToPlainPreview()
	{
		var map = maps.Fresh(42);
		var contract = CreateContract(
			map,
			new AreaIntel("Between {A} and {B}.", "poi-missing-a", "poi-missing-b"),
			new AreaRelation.BetweenLandmarks("poi-missing-a", "poi-missing-b", EAreaDistance.Low));

		var objective = ContractObjectiveProjection.Project(map, contract);

		Assert.IsType<ObjectiveSummaryContent.Plain>(objective.Summary);
	}

	private static Contract CreateContract(StarMap map, AreaIntel intel, AreaRelation relation)
	{
		var plan = map.Blueprint.SupplyPlan;
		return new Contract(
			"contract-test",
			new HuntObjective(
			[
				new SpawnEncounterGroup(
					"group-test",
					new AreaPick(new Coord(4, 0, 4), 12, intel, relation),
					1,
					new FleetSpawnSpec(
						EType.PirateFleet,
						EFaction.Pirates,
						EDangerLevel.VeryLow,
						42,
						[BattleUnitType.Patrol])),
			]),
			map.ControllingFaction,
			plan.AdministrativePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 50)),
			ContractNarrative.ForHunt("Synthetic Hunt"));
	}
}
