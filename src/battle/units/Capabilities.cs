using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Actions;

namespace GrimSpace.Battle.Units;

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
		type switch
		{
			EType.Torpedo => [MoveDef.Instance],
			_ => [..Movement, ..WeaponsFor(type)],
		};

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> WeaponsFor(
		EType type) =>
		type switch
		{
			EType.Fighter =>
			[
				FlakDef.For(EFlakMount.Port),
				FlakDef.For(EFlakMount.Starboard),
				RailgunDef.Instance,
				..TorpedoConfig.EnabledMounts.Select(TorpedoDef.For),
			],
			EType.Patrol => [RailgunDef.Instance],
			EType.Torpedo => [],
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
