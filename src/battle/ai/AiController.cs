using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ai;

public sealed class AiController : SimulationExecutionAgent<BattleWorld, ActorRuntime>
{
	protected override bool PublishOnActivate => false;

	protected override void ProduceActionsJob(Simulation<BattleWorld, ActorRuntime> simulation)
	{
		var session = (BattleSimulation)simulation;
		var actor = UnitRegistry.For(session.World).UnitOf(_actorId!);
		var jobCanWorkGeneration = CanWorkGeneration;
		_ = Task.Run(() =>
		{
			try
			{
				var start = session.Actions.Count;
				TryEnqueueProtectionDrone(session, actor);
				var search = Search(session, actor.State.Id);
				if (search.Count > 0)
				{
					foreach (var action in search.Frames[0].Path)
					{
						if (!session.TryEnqueue(action))
							throw new InvalidOperationException($"Enemy search selected an illegal action: {action}.");
					}
				}
				if (actor.State.Type == EType.Carrier)
					TryAppendRepurposedMinerDeploy(session, actor);
				Publish(session.Actions.Skip(start).ToList(), jobCanWorkGeneration);
			}
			catch (Exception ex)
			{
				Fail(ex, jobCanWorkGeneration);
			}
		});
	}

	internal static IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> SearchCapabilities(
		State actor,
		ActorRuntime runtime) =>
		Capabilities.For(actor, runtime)
			.Where(def =>
				def is not SpawnRepurposedMinerDef
				&& def is not ProtectionDroneDef
				&& (actor.Type != EType.IndustrialGooper || def is not RollDef))
			.ToArray();

	internal static SearchBridge<IEffect<BattleWorld, ActorRuntime>, BattleWorld, ActorRuntime> Search(
		BattleSimulation session,
		string actorId,
		CancellationToken cancellationToken = default,
		TimeSpan? timeBudget = null)
	{
		var budget = timeBudget ?? TimeSpan.FromSeconds(2);
		var dfsTimer = Stopwatch.StartNew();
		var actor = session.World.StateOf(actorId);
		var workers = System.Math.Clamp(Environment.ProcessorCount, 1, 8);
		var search = SearchBridge.FindBest(
			session,
			actorId,
			SearchCapabilities(actor, session.RuntimeFor(actorId)),
			new SearchInput<BattleWorld, ActorRuntime>(
				BattleSearchVisit.ForCapabilitySearch(session),
				ShouldExploreAction: EnemySearchInput.ShouldExploreAction),
			EnemySearchInput.CreateEvaluator(session, actorId),
			workers,
			cancellationToken,
			timeBudget: budget);
		dfsTimer.Stop();
		GameLog.Log(
			$"Enemy DFS ({actorId}): backend=Go workers={search.WorkersUsed}/{workers} "
			+ $"scored={search.EvaluatedFrames} dominancePruned={search.DominancePrunedFrames} "
			+ $"boundPruned={search.BoundPrunedFrames} best={search.BestScore} "
			+ $"dfs={dfsTimer.Elapsed.TotalMilliseconds:F1}ms"
			+ (search.BudgetExhausted
				? $" warning: time budget exhausted (budget={budget.TotalMilliseconds:F1}ms); using best plan found."
				: ""));
		return search;
	}

	private static bool TryEnqueueProtectionDrone(BattleSimulation session, Unit actor)
	{
		var runtime = session.Runtimes.For(actor.State.Id);
		var action = EnemySearchInput.BestProtectionDroneAction(session.World, runtime, actor.State.Id);
		return action is not null && session.TryEnqueue(action);
	}

	private static bool TryAppendRepurposedMinerDeploy(BattleSimulation session, Unit actor)
	{
		var runtime = session.Runtimes.For(actor.State.Id);
		var action = SpawnRepurposedMinerDef.Instance
			.Discover(session.World, runtime, actor.State.Id)
			.OfType<SpawnRepurposedMinerAction>()
			.FirstOrDefault();
		if (action is null)
			return false;

		return session.TryEnqueue(action);
	}
}
