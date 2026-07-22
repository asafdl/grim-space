using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Unit-type registry: which action defs a ship can use. AI and UI ask here first.
/// </summary>
public static class Capabilities
{
	public static IReadOnlyList<IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>> For(
		EType type) =>
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
