using GrimSpace.Battle.World;
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
	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] Movement =
	[
		MoveDef.Instance,
		HeadingDef.Instance,
		RollDef.Instance,
	];

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> For(
		EType type) =>
		[..Movement, ..WeaponsFor(type)];

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> WeaponsFor(
		EType type) =>
		type switch
		{
			EType.Fighter =>
			[
				FlakDef.For(EFlakMount.Port),
				FlakDef.For(EFlakMount.Starboard),
				RailgunDef.Instance,
			],
			EType.Patrol => [RailgunDef.Instance],
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
