using GrimSpace.Battle.Weapons;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public static class Capabilities
{
	public static IReadOnlyList<IActionDef> For(EType type) =>
		type switch
		{
			EType.Fighter =>
			[
				MoveDef.Instance,
				HeadingDef.Instance,
				RollDef.Instance,
				FlakDef.For(EFlakMount.Port),
				FlakDef.For(EFlakMount.Starboard),
				MissileDef.For(EMissileMount.Fore, CombatConfig.ForeMissileMinRange),
				RailgunDef.Instance,
			],
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
