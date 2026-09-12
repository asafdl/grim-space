using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public static class Capabilities
{
	private const string PreviewPatrolId = "__preview_patrol__";
	private const string PreviewTorpedoId = "__preview_torpedo__";

	private static readonly IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>[] Movement =
	[
		MoveDef.Instance,
	];

	public static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> For(
		EType type) =>
		type switch
		{
			EType.Torpedo => [TorpedoMoveDef.Instance, ..AbilitiesFor(EType.Torpedo)],
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

	public static IReadOnlyList<IAction> LegalCapabilities(BattleSimulation sim, string actorId)
	{
		var world = sim.World;
		var runtime = sim.RuntimeFor(actorId);
		var legal = new List<IAction>();

		foreach (var def in AbilitiesFor(world.StateOf(actorId).Type))
		{
			var candidates = def switch
			{
				TorpedoDef torpedo => torpedo.Discover(actorId, PreviewTorpedoId),
				SpawnPatrolDef => [new SpawnPatrolAction(actorId, PreviewPatrolId)],
				_ => def.Discover(world, runtime, actorId),
			};

			foreach (var action in candidates)
			{
				if (sim.Peek(action) is not null)
					legal.Add(action);
			}
		}

		return legal;
	}

	public static bool IsLegalCapability(
		IReadOnlyList<IAction> legalCapabilities,
		IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> def,
		ESpatialOrientation? mountedOn) =>
		legalCapabilities.Any(action =>
			action is IAction<BattleWorld, ActorRuntime> typed
			&& ReferenceEquals(typed.Definition, def)
			&& (action is not IMountedAction mounted
				|| mountedOn == mounted.MountedOn));
}
