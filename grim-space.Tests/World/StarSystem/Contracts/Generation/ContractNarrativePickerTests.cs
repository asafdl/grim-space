using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class ContractNarrativePickerTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_SameInputs_ReturnsDeterministicNarrative()
	{
		var map = maps.Fresh(42);
		var picker = new ContractNarrativePicker();

		var first = picker.Pick(map, EContractKind.Hunt, tick: 10, slotIndex: 0);
		var second = picker.Pick(map, EContractKind.Hunt, tick: 10, slotIndex: 0);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Pick_UsesWholeKindPoolWithoutPresenterContext()
	{
		var map = maps.Fresh(42);
		var config = new ContractNarrativePickerConfig
		{
			Entries =
			[
				new(EContractKind.Hunt, "Generic Hunt", "Generic briefing."),
				new(EContractKind.Hunt, "Command Sweep", "Authority briefing."),
			],
		};
		var picker = new ContractNarrativePicker(config);

		var titles = Enumerable.Range(0, 20)
			.Select(slot => picker.Pick(map, EContractKind.Hunt, tick: 1, slot).Title)
			.ToHashSet(StringComparer.Ordinal);

		Assert.Equal(["Command Sweep", "Generic Hunt"], titles);
	}

	[Fact]
	public void Pick_EmptyPool_FallsBackToDefaults()
	{
		var map = maps.Fresh(42);
		var picker = new ContractNarrativePicker(new ContractNarrativePickerConfig { Entries = [] });

		var narrative = picker.Pick(
			map,
			EContractKind.Delivery,
			tick: 0,
			slotIndex: 0);

		Assert.Equal("Supply Run", narrative.Title);
	}

	[Fact]
	public void Pick_DefaultLeadPool_ProvidesStarterRumorTemplate()
	{
		var map = maps.Fresh(42);
		var picker = new ContractNarrativePicker();

		var narrative = picker.Pick(
			map,
			EContractKind.Lead,
			tick: 0,
			slotIndex: 0);

		Assert.Equal("Quiet Opportunity", narrative.Title);
		Assert.Contains("{destination}", narrative.Briefing);
		Assert.Contains("{merchant}", narrative.Briefing);
	}

	[Fact]
	public void TryPick_ExcludesPendingAndActiveTitles()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var template = TakeHuntTemplate(map);
		map.ContractRegistry.ClearPending();
		Assert.True(map.ContractRegistry.TryAdd(
			CloneHunt(template, "pending-taken", ContractNarrative.ForHunt("Taken Title"))));

		var config = new ContractNarrativePickerConfig
		{
			Entries =
			[
				new(EContractKind.Hunt, "Taken Title", "Already offered."),
				new(EContractKind.Hunt, "Free Title", "Still available."),
			],
		};
		var picker = new ContractNarrativePicker(config);

		Assert.True(picker.TryPick(
			map,
			EContractKind.Hunt,
			tick: 1,
			slotIndex: 0,
			out var narrative));
		Assert.Equal("Free Title", narrative.Title);
	}

	[Fact]
	public void TryPick_ExcludesSupplementalQueuedTitles()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var template = TakeHuntTemplate(map);
		map.ContractRegistry.ClearPending();
		var queued = new[]
		{
			CloneHunt(template, "queued", ContractNarrative.ForHunt("Queued Title")),
		};
		var config = new ContractNarrativePickerConfig
		{
			Entries =
			[
				new(EContractKind.Hunt, "Queued Title", "Already queued."),
				new(EContractKind.Hunt, "Other Title", "Still free."),
			],
		};
		var picker = new ContractNarrativePicker(config);

		Assert.True(picker.TryPick(
			map,
			EContractKind.Hunt,
			tick: 2,
			slotIndex: 0,
			out var narrative,
			queued));
		Assert.Equal("Other Title", narrative.Title);
	}

	[Fact]
	public void TryPick_WhenAllTitlesUsed_ReturnsFalse()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var template = TakeHuntTemplate(map);
		map.ContractRegistry.ClearPending();
		Assert.True(map.ContractRegistry.TryAdd(
			CloneHunt(template, "only", ContractNarrative.ForHunt("Only Title"))));
		var picker = new ContractNarrativePicker(new ContractNarrativePickerConfig
		{
			Entries = [new(EContractKind.Hunt, "Only Title", "Taken.")],
		});

		Assert.False(picker.TryPick(
			map,
			EContractKind.Hunt,
			tick: 3,
			slotIndex: 0,
			out _));
	}

	[Fact]
	public void TryPick_Lead_IgnoresTitleUniqueness()
	{
		var map = maps.Fresh(42);
		var picker = new ContractNarrativePicker(new ContractNarrativePickerConfig
		{
			Entries =
			[
				new(EContractKind.Lead, "Quiet Opportunity", "Already offered."),
			],
		});
		var first = picker.Pick(map, EContractKind.Lead, tick: 1, slotIndex: 0);
		Assert.True(map.ContractRegistry.TryAdd(new Contract(
			"lead-existing",
			new BlackMarketSaleObjective(
				new BlackMarketFacilityDestination("poi-a", "facility-a"),
				EMerchantSubtype.Weapons,
				durationTicks: 10),
			EDangerLevel.VeryLow,
			map.ControllingFaction,
			new ContractTerms(ResourceBundle.Empty),
			first,
			Category: EContractCategory.Lead)));

		Assert.True(picker.TryPick(
			map,
			EContractKind.Lead,
			tick: 2,
			slotIndex: 0,
			out var second));
		Assert.Equal(first.Title, second.Title);
	}

	private static Contract TakeHuntTemplate(StarMap map) =>
		map.ContractRegistry.All.First(contract => contract.Objective is HuntObjective);

	private static Contract CloneHunt(
		Contract template,
		string contractId,
		ContractNarrative narrative) =>
		template with
		{
			Id = contractId,
			Narrative = narrative,
			IsStoryObjective = false,
		};
}
