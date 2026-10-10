using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class ContractPlacementTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_SameInputs_ReturnsSameKind()
	{
		var map = maps.Fresh(42);
		var placement = new ContractPlacement();
		var first = placement.Pick(map, tick: 1, slotIndex: 2);
		var second = placement.Pick(map, tick: 1, slotIndex: 2);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Pick_MixedWeights_UsesAllKindsOverManySlots()
	{
		var map = maps.Fresh(42);
		var placement = new ContractPlacement();
		var kinds = Enumerable.Range(0, 100)
			.Select(slot => placement.Pick(map, tick: 3, slot))
			.Select(decision => Assert.IsType<ContractPlacement.Decision>(decision).Kind)
			.ToHashSet();

		Assert.Equal(
			[EContractKind.Hunt, EContractKind.Delivery, EContractKind.Wreckage],
			kinds);
	}

	[Fact]
	public void Pick_WhenBoardHasOnlyHunts_ReducesHuntWeightGlobally()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var placement = new ContractPlacement();
		var template = map.ContractRegistry.Pending.Single();
		map.ContractRegistry.TryAdd(template with { Id = "hunt-1", IsStoryObjective = false });
		map.ContractRegistry.TryAdd(template with { Id = "hunt-2", IsStoryObjective = false });
		var kinds = Enumerable.Range(0, 1000)
			.Select(slot => placement.Pick(map, tick: 3, slot))
			.Select(decision => Assert.IsType<ContractPlacement.Decision>(decision).Kind)
			.ToArray();

		Assert.True(kinds.Count(kind => kind == EContractKind.Hunt)
			< kinds.Count(kind => kind == EContractKind.Delivery));
		Assert.True(kinds.Count(kind => kind == EContractKind.Hunt)
			< kinds.Count(kind => kind == EContractKind.Wreckage));
	}

	[Fact]
	public void Pick_SupplementalQueuedHunts_AffectGlobalDiversity()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var placement = new ContractPlacement();
		var template = map.ContractRegistry.Pending.Single();
		var queued = Enumerable.Range(0, 2)
			.Select(index => template with
			{
				Id = $"queued-{index}",
				IsStoryObjective = false,
			})
			.ToArray();
		var kinds = Enumerable.Range(0, 500)
			.Select(slot => placement.Pick(map, tick: 2, slot, queued))
			.Select(decision => Assert.IsType<ContractPlacement.Decision>(decision).Kind)
			.ToArray();

		Assert.True(kinds.Count(kind => kind == EContractKind.Hunt)
			< kinds.Count(kind => kind == EContractKind.Delivery));
		Assert.True(kinds.Count(kind => kind == EContractKind.Hunt)
			< kinds.Count(kind => kind == EContractKind.Wreckage));
	}
}
