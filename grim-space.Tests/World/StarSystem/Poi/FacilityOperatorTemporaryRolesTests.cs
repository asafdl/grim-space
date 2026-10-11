using GrimSpace.World.StarSystem.Merchants;
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
	public void Grant_DifferentPriority_PreservesLowerPriorityAssignment()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.DeliveryTurnIn, "contract-1");

		roles.Grant("poi-a.facility", "Operator", EFacilityOperatorRole.Contracts, "contract-2");

		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out var role));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, role);
		Assert.Single(roles.AssignmentsIncludingSuspended(EFacilityOperatorRole.Contracts));
	}

	[Fact]
	public void Grant_ConflictingMerchantSubtypeAtSamePriority_Throws()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			new OperatorInteraction(
				EFacilityOperatorRole.Merchant,
				EMerchantSubtype.Weapons),
			EOperatorAssignmentPriority.Merchant,
			"merchant-1");

		Assert.Throws<InvalidOperationException>(() =>
			roles.Grant(
				"poi-a.facility",
				"Operator",
				new OperatorInteraction(
					EFacilityOperatorRole.Merchant,
					EMerchantSubtype.ShipSupport),
				EOperatorAssignmentPriority.Merchant,
				"merchant-2"));
	}

	[Fact]
	public void HigherPriorityInteraction_SuspendsAndRestoresMerchant()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			new OperatorInteraction(
				EFacilityOperatorRole.Merchant,
				EMerchantSubtype.Weapons),
			EOperatorAssignmentPriority.Merchant,
			"merchant-1",
			acceptsSourcesUntilTick: 300);
		roles.GrantPrimary(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-1");

		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out var activeRole));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, activeRole);
		var suspended = Assert.Single(
			roles.AssignmentsIncludingSuspended(EFacilityOperatorRole.Merchant));
		Assert.Equal(EMerchantSubtype.Weapons, suspended.MerchantSubtype);

		roles.RevokeBySource("delivery-1");

		Assert.True(roles.TryGetAssignment("poi-a.facility", "Operator", out var restored));
		Assert.Equal(EFacilityOperatorRole.Merchant, restored.Role);
		Assert.Equal(EMerchantSubtype.Weapons, restored.MerchantSubtype);
		Assert.Equal(300, restored.AcceptsSourcesUntilTick);
	}

	[Fact]
	public void SnapshotRestore_PreservesSuspendedMerchant()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			new OperatorInteraction(
				EFacilityOperatorRole.Merchant,
				EMerchantSubtype.Ships),
			EOperatorAssignmentPriority.Merchant,
			"merchant-1",
			acceptsSourcesUntilTick: 300);
		roles.GrantPrimary(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-1");

		var restored = FacilityOperatorTemporaryRoles.FromSnapshot(roles.Snapshot());
		restored.RevokeBySource("delivery-1");

		Assert.True(restored.TryGetAssignment("poi-a.facility", "Operator", out var merchant));
		Assert.Equal(EMerchantSubtype.Ships, merchant.MerchantSubtype);
		Assert.Equal(EOperatorAssignmentPriority.Merchant, merchant.Priority);
		Assert.Equal(300, merchant.AcceptsSourcesUntilTick);
	}

	[Fact]
	public void CloneForFork_MerchantMutationIsIndependent()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			new OperatorInteraction(
				EFacilityOperatorRole.Merchant,
				EMerchantSubtype.Weapons),
			EOperatorAssignmentPriority.Merchant,
			"merchant-1");

		var clone = roles.CloneForFork();
		clone.RevokeBySource("merchant-1");

		Assert.False(clone.TryGetInteraction("poi-a.facility", "Operator", out _));
		Assert.True(roles.TryGetInteraction("poi-a.facility", "Operator", out var original));
		Assert.Equal(EMerchantSubtype.Weapons, original.MerchantSubtype);
	}

	[Fact]
	public void GrantPrimary_SuspendsAndRestoresExistingRole()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-offer",
			acceptsSourcesUntilTick: 10);

		roles.GrantPrimary(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-contract");

		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out var role));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, role);
		Assert.Empty(roles.Assignments(EFacilityOperatorRole.Contracts));

		roles.RevokeBySource("delivery-contract");

		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out role));
		Assert.Equal(EFacilityOperatorRole.Contracts, role);
		var restored = Assert.Single(roles.Assignments(EFacilityOperatorRole.Contracts));
		Assert.Equal(["contract-offer"], restored.SourceIds);
		Assert.Equal(10, restored.AcceptsSourcesUntilTick);
	}

	[Fact]
	public void CloneForFork_CopiesSuspendedRole()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-offer",
			acceptsSourcesUntilTick: 10);
		roles.GrantPrimary(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-contract");

		var clone = roles.CloneForFork();
		clone.RevokeBySource("delivery-contract");

		Assert.True(clone.TryGetRole("poi-a.facility", "Operator", out var cloneRole));
		Assert.Equal(EFacilityOperatorRole.Contracts, cloneRole);
		Assert.True(roles.TryGetRole("poi-a.facility", "Operator", out var originalRole));
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, originalRole);
	}

	[Fact]
	public void PruneSources_RemovesSuspendedRoleSource()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.Contracts,
			"contract-offer",
			acceptsSourcesUntilTick: 10);
		roles.GrantPrimary(
			"poi-a.facility",
			"Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"delivery-contract");

		roles.PruneSources(
			EFacilityOperatorRole.Contracts,
			new HashSet<string>(StringComparer.Ordinal));
		roles.RevokeBySource("delivery-contract");

		Assert.False(roles.TryGetRole("poi-a.facility", "Operator", out _));
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

	[Fact]
	public void Snapshot_OmitsContractAssignmentsButKeepsDeliveryRoles()
	{
		var roles = new FacilityOperatorTemporaryRoles();
		roles.Grant(
			"poi-a.contracts",
			"Contract Operator",
			EFacilityOperatorRole.Contracts,
			"contract-1",
			acceptsSourcesUntilTick: 10);
		roles.Grant(
			"poi-a.delivery",
			"Delivery Operator",
			EFacilityOperatorRole.DeliveryTurnIn,
			"contract-2");

		var snapshot = roles.Snapshot();

		var entry = Assert.Single(snapshot);
		Assert.Equal("poi-a.delivery", entry.FacilityId);
		Assert.Equal(EFacilityOperatorRole.DeliveryTurnIn, entry.Role);
		Assert.Equal("contract-2", entry.SourceId);
	}

	[Fact]
	public void FromSnapshot_DropsLegacyGeneratedContractAssignments()
	{
		var roles = FacilityOperatorTemporaryRoles.FromSnapshot(
		[
			("poi-a.contracts", "Contract Operator", EFacilityOperatorRole.Contracts, "contract-1"),
			("poi-a.story", "Story Contact", EFacilityOperatorRole.StoryContact, "story-1"),
		]);

		Assert.Empty(roles.Assignments(EFacilityOperatorRole.Contracts));
		Assert.Equal(
			["story-1"],
			Assert.Single(roles.Assignments(EFacilityOperatorRole.StoryContact)).SourceIds);
	}
}
