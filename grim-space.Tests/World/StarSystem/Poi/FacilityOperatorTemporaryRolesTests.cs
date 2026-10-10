using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
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
	public void Grant_SameRoleFromDifferentSources_RemainsUntilBothRevoke()
	{
		var roles = new FacilityOperatorTemporaryRoles();

		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-1");
		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-2");

		roles.RevokeBySource("contract-1");
		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out var role));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, role);

		roles.RevokeBySource("contract-2");
		Assert.False(roles.TryGetRole("poi-a.facility", "Operator", out _));
	}

	[Fact]
	public void Grant_DifferentRoleFromDifferentSource_Throws()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-1");

		Assert.Throws<InvalidOperationException>(() =>
			roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.Contracts, "contract-2"));
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

	[Fact]
	public void LeasedAssignment_AcceptsSourcesOnlyBeforeDeadline()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-1",
			acceptsSourcesUntilTick: 10);

		Assert.True(roles.IsAcceptingSources(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			currentTick: 9));
		Assert.False(roles.IsAcceptingSources(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			currentTick: 10));

		var assignment = Assert.Single(roles.Assignments(EFacilityOperatorRole.Contracts));
		Assert.Equal(10, assignment.AcceptsSourcesUntilTick);
		Assert.Equal(["contract-1"], assignment.SourceIds);
	}

	[Fact]
	public void RenewSourceAcceptance_UpdatesExpiredDeadline()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-1",
			acceptsSourcesUntilTick: 10);

		roles.RenewSourceAcceptance("poi-a.facility", "Operator", acceptsSourcesUntilTick: 20);

		Assert.True(roles.IsAcceptingSources(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			currentTick: 10));
		Assert.Equal(
			20,
			Assert.Single(roles.Assignments(EFacilityOperatorRole.Contracts))
				.AcceptsSourcesUntilTick);
	}

	[Fact]
	public void ContractPlacementPause_RemainsAfterRoleClears()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-1",
			acceptsSourcesUntilTick: 10);
		roles.PauseContractPlacement("poi-a.facility", "Operator", untilTick: 20);

		roles.RevokeBySource("contract-1");

		Assert.False(roles.TryGetRole("poi-a.facility", "Operator", out _));
		Assert.True(roles.IsContractPlacementPaused(
			"poi-a.facility",
			"Operator",
			currentTick: 19));
		Assert.False(roles.IsContractPlacementPaused(
			"poi-a.facility",
			"Operator",
			currentTick: 20));
	}

	[Fact]
	public void PruneSources_RemovesOnlyMatchingRoleSources()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.contracts",
			"Contract Operator",
			EFacilityOperatorRole.Contracts,
			"contract-1",
			acceptsSourcesUntilTick: 10);
		roles.Grant(
			"poi-a.contracts-2",
			"Second Contract Operator",
			EFacilityOperatorRole.Contracts,
			"contract-2",
			acceptsSourcesUntilTick: 10);
		roles.Grant(
			"poi-a.delivery",
			"Delivery Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"contract-1");

		roles.PruneSources(
			EFacilityOperatorRole.Contracts,
			new HashSet<string>(["contract-2"], StringComparer.Ordinal));

		var assignment = Assert.Single(roles.Assignments(EFacilityOperatorRole.Contracts));
		Assert.Equal("Second Contract Operator", assignment.OperatorName);
		Assert.True(roles.TryGetRole(
			"poi-a.delivery",
			"Delivery Operator",
			out var deliveryRole));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, deliveryRole);
	}

	[Fact]
	public void CloneForFork_CopiesLeaseAndPlacementPause()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-1",
			acceptsSourcesUntilTick: 10);
		roles.PauseContractPlacement("poi-a.facility", "Operator", untilTick: 20);

		var clone = roles.CloneForFork();

		Assert.Equal(
			10,
			Assert.Single(clone.Assignments(EFacilityOperatorRole.Contracts))
				.AcceptsSourcesUntilTick);
		Assert.True(clone.IsContractPlacementPaused(
			"poi-a.facility",
			"Operator",
			currentTick: 19));
	}
}
