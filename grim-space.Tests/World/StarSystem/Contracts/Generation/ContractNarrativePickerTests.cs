using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
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
}
