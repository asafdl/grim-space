using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Player turn planning state: queued actions and the committed snapshot they diff against.
/// </summary>
public sealed class PlayerPlan
{
	private readonly PlanQueue<IBattleAction> _actions = new();

	public GridBasis StartFacing { get; private set; }

	public IReadOnlyList<IBattleAction> Actions => _actions.Actions;

	public void ResetFrom(State player)
	{
		StartFacing = GridBasis.From(
			player.ForwardDirection,
			player.UpDirection,
			player.RightDirection);
		_actions.Clear();
	}

	public void Enqueue(IBattleAction action) => _actions.Enqueue(action);

	public bool TryUndoLast() => _actions.TryPopLast(out _);

	public void Clear() => _actions.Clear();

	public BattlePlanContext Context => new(_actions.Actions, StartFacing);
}
