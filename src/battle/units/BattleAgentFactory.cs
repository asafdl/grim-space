using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Units;

public static class BattleAgentFactory
{
	public static EBattleAgentKind KindOf(ExecutionAgent<BattleWorld, ActorRuntime> agent) =>
		agent switch
		{
			UserExecutionAgent => EBattleAgentKind.Player,
			VoidBombExecutionAgent => EBattleAgentKind.VoidBomb,
			AiController => EBattleAgentKind.Ai,
			_ => throw new ArgumentOutOfRangeException(
				nameof(agent),
				agent.GetType().Name,
				"Unknown battle execution agent."),
		};

	public static ExecutionAgent<BattleWorld, ActorRuntime> Create(EBattleAgentKind kind) =>
		kind switch
		{
			EBattleAgentKind.Player => new UserExecutionAgent(),
			EBattleAgentKind.Ai => new AiController(),
			EBattleAgentKind.VoidBomb => new VoidBombExecutionAgent(),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown battle agent kind."),
		};
}
