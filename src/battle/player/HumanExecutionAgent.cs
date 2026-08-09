using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Ai;

public sealed class HumanExecutionAgent : IExecutionAgent<BattleWorld, ActorRuntime, Unit>
{
	public BattleSimulation Sim { get; private set; } = null!;

	public void BeginTurn(Func<BattleSimulation> createSim) =>
		Sim = createSim();

	public Task<IReadOnlyList<IAction>> GetActionsAsync(
		Unit actor,
		Func<BattleSimulation> createSim)
	{
		if (!Sim.TryCommit(out var actions, out _))
			return Task.FromResult<IReadOnlyList<IAction>>([]);

		IReadOnlyList<IAction> streamlined =
			OrientationStreamline.Compact(actions, Sim.UndoGroups);
		return Task.FromResult(streamlined);
	}
}
