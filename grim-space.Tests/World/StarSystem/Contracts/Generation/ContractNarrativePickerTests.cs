using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

public sealed class ContractNarrativePickerTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_SameInputs_ReturnsDeterministicNarrative()
	{
		var map = maps.Fresh(42);
		var issuerPoiId = map.Blueprint.SupplyPlan.AdministrativePoiId;
		var picker = new ContractNarrativePicker();

		var first = picker.Pick(map, issuerPoiId, EContractKind.Hunt, tick: 10, slotIndex: 0);
		var second = picker.Pick(map, issuerPoiId, EContractKind.Hunt, tick: 10, slotIndex: 0);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Pick_AdminIssuer_PrefersFacilitySpecificHuntEntry()
	{
		var map = maps.Fresh(42);
		var issuerPoiId = map.Blueprint.SupplyPlan.AdministrativePoiId;
		var config = new ContractNarrativePickerConfig
		{
			Entries =
			[
				new(EContractKind.Hunt, "Generic Hunt", "Generic briefing."),
				new(
					EContractKind.Hunt,
					"Command Sweep",
					"Authority briefing.",
					FacilitySlug: AdministrativeCore.ManagementFacilitySlug),
			],
		};
		var picker = new ContractNarrativePicker(config);

		var narrative = picker.Pick(map, issuerPoiId, EContractKind.Hunt, tick: 1, slotIndex: 0);

		Assert.Equal("Command Sweep", narrative.Title);
		Assert.Equal("Authority briefing.", narrative.Briefing);
	}

	[Fact]
	public void Pick_UnknownIssuer_UsesGenericPoolEntries()
	{
		var map = maps.Fresh(42);
		var config = new ContractNarrativePickerConfig
		{
			Entries =
			[
				new(
					EContractKind.Delivery,
					"Command Freight",
					"Only for command.",
					"Done.",
					FacilitySlug: AdministrativeCore.ManagementFacilitySlug),
				new(EContractKind.Delivery, "Open Freight", "Any port.", "Received."),
			],
		};
		var picker = new ContractNarrativePicker(config);

		var narrative = picker.Pick(map, "missing-poi", EContractKind.Delivery, tick: 2, slotIndex: 1);

		Assert.Equal("Open Freight", narrative.Title);
	}

	[Fact]
	public void Pick_EmptyPool_FallsBackToDefaults()
	{
		var map = maps.Fresh(42);
		var picker = new ContractNarrativePicker(new ContractNarrativePickerConfig { Entries = [] });

		var narrative = picker.Pick(
			map,
			map.Blueprint.SupplyPlan.AdministrativePoiId,
			EContractKind.Delivery,
			tick: 0,
			slotIndex: 0);

		Assert.Equal("Supply Run", narrative.Title);
	}
}
