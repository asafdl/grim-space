namespace GrimSpace.World.StarSystem.Poi;

public static class OperatorDisplayLabels
{
	public static string Role(EFacilityOperatorRole role) => role switch
	{
		EFacilityOperatorRole.Contracts => "Contracts",
		EFacilityOperatorRole.DockyardShop => "Dockyard Shop",
		EFacilityOperatorRole.ShieldRecharge => "Shield Recharge",
		EFacilityOperatorRole.Dialog => "",
		_ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
	};

	public static string Title(FacilityOperator facilityOperator)
	{
		var role = Role(facilityOperator.Role);
		return string.IsNullOrEmpty(role)
			? facilityOperator.Name
			: $"{facilityOperator.Name} - {role}";
	}
}
