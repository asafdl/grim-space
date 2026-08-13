using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ai;

internal enum ETorpedoTargetClass
{
	Unreachable = 0,
	Future = 1,
	InTrajectory = 2,
}

public sealed class TorpedoReachEnvelope(IReadOnlyList<IReadOnlySet<Coord>> layers)
{
	public IReadOnlyList<IReadOnlySet<Coord>> Layers { get; } = layers;

	public int Count => Layers.Count;

	public bool WithinBlast(int turn, Coord target)
	{
		if (turn < 0 || turn >= Layers.Count)
			return false;

		foreach (var position in Layers[turn])
		{
			if (position.ManhattanDistanceTo(target) <= TorpedoConfig.BlastRadius)
				return true;
		}

		return false;
	}

	internal ETorpedoTargetClass Classify(Coord target)
	{
		if (WithinBlast(0, target))
			return ETorpedoTargetClass.InTrajectory;

		for (var turn = 1; turn < Count; turn++)
		{
			if (WithinBlast(turn, target))
				return ETorpedoTargetClass.Future;
		}

		return ETorpedoTargetClass.Unreachable;
	}

	public int EarliestReachTurn(Coord target)
	{
		for (var turn = 0; turn < Count; turn++)
		{
			if (WithinBlast(turn, target))
				return turn;
		}

		return int.MaxValue;
	}

	public static TorpedoReachEnvelope Build(BattleSimulation session, string actorId)
	{
		var fuel = session.StateOf<ActorState>(actorId).FuelRemaining;
		if (fuel <= 0)
			return new TorpedoReachEnvelope([]);

		IReadOnlyList<IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>> capabilities =
			[MoveDef.Instance];
		var frontiers = new List<(BattleWorld World, ActorRuntimes<ActorRuntime> Runtimes)>
		{
			(session.World.Fork(), session.Runtimes.Fork()),
		};
		var layers = new List<IReadOnlySet<Coord>>(fuel);

		for (var turn = 0; turn < fuel; turn++)
		{
			var positions = new HashSet<Coord>();
			var nextFrontiers = new Dictionary<(Coord Position, int Momentum), (BattleWorld, ActorRuntimes<ActorRuntime>)>();

			foreach (var (world, runtimes) in frontiers)
			{
				var sim = new BattleSimulation(world, runtimes);
				sim.Begin(session.AnchorTick, session.WorldVersion);

				foreach (var frame in ActionSearch.Run(
					sim,
					actorId,
					capabilities,
					BattleSearchVisit.ForMove))
				{
					var state = frame.World.StateOf(actorId);
					var path = frame.Runtimes.For(actorId).ActivePath;
					if (path is not null && !path.CanEnd(state.Stats.MinPathApCost))
						continue;

					positions.Add(state.Position);

					if (turn + 1 >= fuel)
						continue;

					var nextWorld = frame.World.Fork();
					var nextRuntimes = frame.Runtimes.Fork();
					ExecutionHelper.Apply(new EndOfPhaseAction(actorId), nextWorld, nextRuntimes.For(actorId));
					ExecutionHelper.Apply(new RoundUpkeepAction(actorId), nextWorld, nextRuntimes.For(actorId));

					var nextState = nextWorld.StateOf(actorId);
					nextFrontiers[(nextState.Position, nextState.MomentumLevel)] = (nextWorld, nextRuntimes);
				}
			}

			layers.Add(positions);
			frontiers = nextFrontiers.Values.ToList();
			if (frontiers.Count == 0)
				break;
		}

		return new TorpedoReachEnvelope(layers);
	}
}
