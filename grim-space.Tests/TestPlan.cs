using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal sealed class TestPlan
{
	private readonly PlanSimulation _sim;
	private readonly string _ownerId;

	private TestPlan(PlanSimulation sim, string ownerId)
	{
		_sim = sim;
		_ownerId = ownerId;
	}

	public PlanSimulation Simulation => _sim;
	public BattleBoard Board => _sim.PreviewWorld;
	public IReadOnlyList<IBattleAction> Actions => _sim.Actions;
	public BattleActionContext Context => BattleActionContext.For(_sim.PreviewWorld, _sim.PreviewRuntime, _ownerId);
	public Timeline PreviewTimeline => Board.Timeline;
	public int TurnStartTick => _sim.AnchorTick;

	public bool TryApplyAndEnqueue(IAction action) =>
		action is IBattleAction battleAction && _sim.TryEnqueue(battleAction);

	public void ForceApplyAndEnqueue(IAction action)
	{
		if (action is IBattleAction battleAction)
			_sim.ForceEnqueue(battleAction);
	}

	public bool TryEnqueueMovePath(string actorId, Option option) =>
		TryApplyAndEnqueue(new MovePathAction(actorId, option));

	public bool TryUndoLast() => _sim.TryUndoLast();

	public void AdvanceToTick(int tick) =>
		_sim.AdvanceToTick(tick, scheduled =>
		{
			var ctx = BattleActionContext.For(_sim.PreviewWorld, _sim.PreviewRuntime, scheduled.OwnerId);
			SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, scheduled);
		});

	public void EnqueueMovePath(Option option)
	{
		ForceApplyAndEnqueue(new MovePathAction(_ownerId, option));
		_sim.Refresh();
	}

	public void CopyFrom(IEnumerable<IAction> actions) => _sim.CopyActionsFrom(actions.Cast<IBattleAction>());

	public SimulatedTurn GetPreview(string actorId) =>
		new() { Board = _sim.PreviewWorld, ActorId = actorId };

	public static TestPlan Begin(
		string ownerId,
		Coord origin,
		int momentum = 0,
		Coord? enemyOrigin = null,
		int turnStartTick = 0)
	{
		var player = BattleTestFixture.Player(origin, momentum: momentum);
		var enemy = BattleTestFixture.Enemy(enemyOrigin ?? new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var sim = new PlanSimulation(actions => BattlePlayback.WithPhaseEnd(actions, ownerId));
		sim.Begin(
			BattleBoard.FromSnapshot([player, enemy], new Dictionary<string, NonUnit>(), grid, blocked),
			turnStartTick);
		return new TestPlan(sim, ownerId);
	}

	public static TestPlan Begin(
		string ownerId,
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		IReadOnlySet<Coord> blocked,
		int turnStartTick = 0)
	{
		var sim = new PlanSimulation(actions => BattlePlayback.WithPhaseEnd(actions, ownerId));
		sim.Begin(
			BattleBoard.FromSnapshot([player, enemy], new Dictionary<string, NonUnit>(), grid, blocked),
			turnStartTick);
		return new TestPlan(sim, ownerId);
	}
}
