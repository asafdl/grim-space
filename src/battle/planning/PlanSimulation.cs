using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Planning;

public sealed class PlanSimulation : Simulation<BattleBoard, TurnPhaseContext, BattleActionContext, BattleSlices>
{
	private string? _playbackOwnerId;

	public void Begin(BattleBoard anchorWorld, TurnPhaseContext anchorContext, int anchorTick, string? playbackOwnerId = null)
	{
		_playbackOwnerId = playbackOwnerId;
		base.Begin(anchorWorld, anchorContext, anchorTick);
	}

	public IEnumerable<IAction> Discover(string ownerId, EType unitType)
	{
		var ctx = CreateContext(PreviewWorld, PreviewRuntime, ownerId);
		foreach (var def in Capabilities.For(unitType))
		foreach (var action in def.Discover(ctx, ownerId))
			yield return action;
	}

	protected override BattleActionContext CreateContext(BattleBoard world, TurnPhaseContext runtime, string ownerId) =>
		BattleActionContext.For(world, runtime, ownerId);

	protected override bool IsActionLegal(BattleActionContext ctx, IAction action) =>
		action is IBattleAction battleAction && battleAction.IsLegal(ctx);

	protected override void ApplyAction(BattleActionContext ctx, IAction action)
	{
		if (action is IBattleAction battleAction)
			SimulationRunner<BattleActionContext, BattleSlices, IBattleAction>.Step(ctx, battleAction);
	}

	protected override IReadOnlyList<IAction> ExpandPlayback(IReadOnlyList<IAction> actions) =>
		BattlePlayback.WithPhaseEnd(actions, _playbackOwnerId);
}
