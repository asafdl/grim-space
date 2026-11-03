using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ActivateShieldBraceEffect(ESpatialOrientation face) : IEffect<BattleWorld, ActorRuntime>
{
	private BattleStatus? _previousStatus;
	private bool _previousUsedThisTurn;
	private FaceShieldPoints? _previousShieldPoints;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousStatus = ShieldBrace.TryStatus(actor)?.DeepCopy();
		_previousUsedThisTurn = actor.ShieldBraceUsedThisTurn;
		_previousShieldPoints = actor.CurrentShieldPoints.Clone();
		ShieldBrace.Activate(actor, face);
		return [new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(actor))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ShieldBraceUsedThisTurn = _previousUsedThisTurn;
		actor.CurrentShieldPoints = _previousShieldPoints!.Clone();
		ShieldBrace.Restore(actor, _previousStatus?.DeepCopy());
	}
}
