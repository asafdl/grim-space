using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DeliveryDropoffPickerTests(StarMapFixture maps)
{
	[Fact]
	public void Pick_SameSeedAndContractId_IsDeterministic()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		const string contractId = "contract-test-dropoff";

		var excluded = new HashSet<string>(StringComparer.Ordinal) { plan.StoragePoiId };
		var first = DeliveryDropoffPicker.Pick(map, contractId, "delivery-dropoff", excluded);
		var second = DeliveryDropoffPicker.Pick(map, contractId, "delivery-dropoff", excluded);

		Assert.Equal(first, second);
	}

	[Fact]
	public void Pick_ExcludesIssuerPoi()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;

		for (var index = 0; index < 8; index++)
		{
			var excluded = new HashSet<string>(StringComparer.Ordinal) { plan.StoragePoiId };
			var dropoff = DeliveryDropoffPicker.Pick(map, $"contract-{index}", "delivery-dropoff", excluded);
			Assert.NotEqual(plan.StoragePoiId, dropoff.PoiId);
		}
	}
}
