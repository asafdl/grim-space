using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public static class OperatorClassCompatibility
{
	public static string DisplayName(this EOperatorClassId operatorClass) => operatorClass switch
	{
		EOperatorClassId.SpaceRanger => "Space Ranger",
		EOperatorClassId.Support => "Forward Deployed Engineer",
		EOperatorClassId.Unclassified => "Unclassified",
		_ => throw new ArgumentOutOfRangeException(nameof(operatorClass), operatorClass, null),
	};

	public static bool CanOperate(EOperatorClassId operatorClass, EOperatorClassId shipClass) =>
		Enum.IsDefined(operatorClass)
		&& Enum.IsDefined(shipClass)
		&& operatorClass != EOperatorClassId.Unclassified
		&& operatorClass == shipClass;
}
