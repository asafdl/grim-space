using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

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

	public static IEnumerable<IAction> Discover(
		BattleBoard world,
		ActorSession runtime,
		string ownerId,
		EType unitType)
	{
		foreach (var def in For(unitType))
		foreach (var action in def.Discover(world, runtime, ownerId))
			yield return action;
	}
}
