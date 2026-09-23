using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class ContractPlacementTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_FreshMap_UsesBothContractIssuersOverManySlots()
	{
		var map = maps.Fresh(42);
		var placement = new ContractPlacement();
		var plan = map.Blueprint.SupplyPlan;
		var issuers = new HashSet<string>(StringComparer.Ordinal);

		for (var slot = 0; slot < 24; slot++)
		{
			var decision = placement.Pick(map, tick: 1, slot);
			Assert.NotNull(decision);
			issuers.Add(decision.IssuerPoiId);
		}

		Assert.Contains(plan.AdministrativePoiId, issuers);
		Assert.Contains(plan.ExtractionPoiId, issuers);
	}

	[Fact]
	public void Pick_TwoIssuers_UsesBothOverManySlots()
	{
		var map = ContractPlacementTestMaps.TwoIssuers();
		var placement = new ContractPlacement();
		var issuers = new HashSet<string>(StringComparer.Ordinal);

		for (var slot = 0; slot < 24; slot++)
		{
			var decision = placement.Pick(map, tick: 5, slot);
			Assert.NotNull(decision);
			issuers.Add(decision.IssuerPoiId);
		}

		Assert.Contains(ContractPlacementTestMaps.IssuerAId, issuers);
		Assert.Contains(ContractPlacementTestMaps.IssuerBId, issuers);
	}

	[Fact]
	public void Pick_RespectsMaxPendingPerIssuerPoi()
	{
		var map = ContractPlacementTestMaps.TwoIssuers();
		var config = new ContractPlacementConfig { TargetGeneratedCount = 3 };
		var placement = new ContractPlacement(config);
		var hunt = SyntheticHunt(map, "prefill-group");

		for (var i = 0; i < config.MaxPendingPerIssuerPoi(2); i++)
			map.ContractRegistry.TryAdd(CloneAtIssuer(map, $"a-{i}", hunt, ContractPlacementTestMaps.IssuerAId));

		var decision = placement.Pick(map, tick: 2, slotIndex: 0);
		Assert.NotNull(decision);
		Assert.Equal(ContractPlacementTestMaps.IssuerBId, decision.IssuerPoiId);
	}

	[Fact]
	public void Pick_WhenIssuerHasOnlyHunts_PrefersDelivery()
	{
		var map = ContractPlacementTestMaps.TwoIssuers();
		var placement = new ContractPlacement();
		var hunt = SyntheticHunt(map, "stacked-group");
		map.ContractRegistry.TryAdd(CloneAtIssuer(map, "hunt-1", hunt, ContractPlacementTestMaps.IssuerAId));
		map.ContractRegistry.TryAdd(CloneAtIssuer(map, "hunt-2", hunt, ContractPlacementTestMaps.IssuerAId));

		var decision = placement.Pick(map, tick: 3, slotIndex: 4);
		Assert.NotNull(decision);
		if (decision.IssuerPoiId == ContractPlacementTestMaps.IssuerAId)
			Assert.Equal(EContractKind.Delivery, decision.Kind);
	}

	[Fact]
	public void MaxPendingPerIssuerPoi_IsTargetMinusOneWhenMultipleIssuers()
	{
		var config = new ContractPlacementConfig { TargetGeneratedCount = 4 };
		Assert.Equal(3, config.MaxPendingPerIssuerPoi(2));
		Assert.Equal(int.MaxValue, config.MaxPendingPerIssuerPoi(1));
	}

	private static HuntObjective SyntheticHunt(StarMap map, string groupId)
	{
		var searchArea = new AreaPick(
			new Coord(map.Width / 2, 0, map.Height / 2),
			32,
			new AreaIntel(
				"Somewhere in the area of {A}.",
				ContractPlacementTestMaps.IssuerAId,
				ContractPlacementTestMaps.IssuerBId,
				ContractPlacementTestMaps.IssuerAId),
			default!);
		return new HuntObjective(
		[
			new SpawnEncounterGroup(
				groupId,
				searchArea,
				1,
				new FleetSpawnSpec(
					FleetType.PirateFleet,
					EFaction.Pirates,
					EDangerLevel.VeryLow,
					1,
					[BattleUnitType.Patrol])),
		]);
	}

	private static Contract CloneAtIssuer(
		StarMap map,
		string contractId,
		HuntObjective hunt,
		string issuerPoiId) =>
		new(
			contractId,
			hunt,
			map.ControllingFaction,
			issuerPoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, 10)),
			ContractNarrative.ForHunt("Test"));
}
