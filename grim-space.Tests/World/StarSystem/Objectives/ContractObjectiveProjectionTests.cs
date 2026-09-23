using GrimSpace.Math.Routes;
using GrimSpace.Math.Grid;
using GrimSpace.Tutorials;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Objectives;

[StarSystemTestSuite]
public sealed class ContractObjectiveProjectionTests(StarMapFixture maps)
{
	[Fact]
	public void Project_StarterContract_UsesPersistedIntelTitleAndReward()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contract = map.ContractRegistry.Pending.Single();
		var objective = ContractObjectiveProjection.Project(map, contract);
		var hunt = (HuntObjective)contract.Objective;
		var searchArea = hunt.SpawnGroups[0].SearchArea;
		Assert.True(AreaIntelDisplay.TryParseLinkableSegments(searchArea.Intel, out _));
		AssertIntelLandmarksOrderedByProximity(map, searchArea);

		Assert.Equal("Pirate Hunt ★", objective.Title);
		Assert.False(objective.Summary is ObjectiveSummaryContent.Plain);
		Assert.Equal(
			ContractDisplay.SearchArea(contract, map),
			AreaIntelDisplay.FormatPlain(
				searchArea.Intel,
				id => MapLandmarkQueries.GetDisplayName(map, id)));
		Assert.True(objective.Reward.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(TutorialBeatContracts.BeatAHuntRewardCredits, credits);
	}

	[Fact]
	public void Project_NonLinkableIntel_FallsBackToPlainPreview()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contract = CreateContract(
			map,
			new AreaIntel("No landmarks here.", "poi-a", "poi-b", "poi-c"),
			null!);

		var objective = ContractObjectiveProjection.Project(map, contract);

		var plain = Assert.IsType<ObjectiveSummaryContent.Plain>(objective.Summary);
		Assert.Equal(ContractDisplay.ObjectivePreview(contract, map), plain.Text);
	}

	[Fact]
	public void Project_MissingLandmarkId_FallsBackToPlainPreview()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contract = CreateContract(
			map,
			new AreaIntel("Near {A}.", "poi-missing-a", "poi-missing-b", "poi-missing-c"),
			new AreaRelation.TriangulatedLandmarks("poi-missing-a", "poi-missing-b", "poi-missing-c"));

		var objective = ContractObjectiveProjection.Project(map, contract);

		Assert.IsType<ObjectiveSummaryContent.Plain>(objective.Summary);
	}

	private static void AssertIntelLandmarksOrderedByProximity(StarMap map, AreaPick searchArea)
	{
		var center = searchArea.Center;
		var intel = searchArea.Intel;
		var ids = new[] { intel.LandmarkAId, intel.LandmarkBId, intel.LandmarkCId };
		MapLandmarkQueries.TryGet(map, ids[0], out var a);
		MapLandmarkQueries.TryGet(map, ids[1], out var b);
		MapLandmarkQueries.TryGet(map, ids[2], out var c);

		var distances = new[]
		{
			RouteGeometry.Distance(center, a.Position),
			RouteGeometry.Distance(center, b.Position),
			RouteGeometry.Distance(center, c.Position),
		};

		Assert.True(distances[0] <= distances[1]);
		Assert.True(distances[1] <= distances[2]);
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
