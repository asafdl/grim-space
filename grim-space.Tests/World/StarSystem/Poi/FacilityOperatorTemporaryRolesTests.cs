using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Poi;

public sealed class FacilityOperatorTemporaryRolesTests
{
	[Fact]
	public void GrantAndRevokeBySource_ClearsOverlay()
	{
		var roles = new FacilityOperatorTemporaryRoles();

		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-1");
		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out var role));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, role);

		roles.RevokeBySource("contract-1");
		Assert.False(roles.TryGetRole("poi-a.facility", "Operator", out _));
	}

	[Fact]
	public void Grant_DifferentSourceOnSameOperator_Throws()
	{
		var roles = new FacilityOperatorTemporaryRoles();

		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-1");

		Assert.Throws<InvalidOperationException>(() =>
			roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-2"));
	}

	[Fact]
	public void CloneForFork_CopiesOverlays()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-1");

		var clone = roles.CloneForFork();

		Assert.True(clone.TryGetRole("poi-a.facility", "Operator", out var role));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, role);
	}
}
