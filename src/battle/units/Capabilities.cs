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
			EType.Torpedo => [MoveDef.Instance, ..AbilitiesFor(EType.Torpedo)],
			_ => [..Movement, ..AbilitiesFor(type)],
		};

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> AbilitiesFor(
		EType type) =>
		type switch
		{
			EType.Fighter =>
			[
				FlakDef.Instance,
				RailgunDef.Instance,
				TorpedoDef.Instance,
			],
			EType.Carrier => [RailgunDef.Instance, SpawnPatrolDef.Instance],
			EType.Patrol => [FlakDef.Instance],
			EType.Torpedo => [DetonateDef.Instance],
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
}
