using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
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
			_ => [..Movement, ..AbilitiesFor(type)],
		};

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilitiesFor(
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
			EType.Carrier => [RailgunDef.Instance],
			EType.Patrol =>
			[
				FlakDef.For(EFlakMount.Port),
				FlakDef.For(EFlakMount.Starboard),
			],
			EType.Torpedo => [],
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
