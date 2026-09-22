using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class DeliveryDropoffPickerTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_SameSeedAndContractId_IsDeterministic()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		const string contractId = "contract-test-dropoff";

		var first = DeliveryDropoffPicker.Pick(map, plan.StoragePoiId, contractId);
		var second = DeliveryDropoffPicker.Pick(map, plan.StoragePoiId, contractId);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Pick_ExcludesIssuerPoi()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;

		for (var index = 0; index < 8; index++)
		{
			var dropoff = DeliveryDropoffPicker.Pick(map, plan.StoragePoiId, $"contract-{index}");
			Assert.NotEqual(plan.StoragePoiId, dropoff.PoiId);
		}
	}
}
