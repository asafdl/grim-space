using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ActivateShieldBraceEffect(ESpatialOrientation face) : IEffect<BattleWorld, ActorRuntime>
{
	private bool _previousActive;
	private ESpatialOrientation? _previousFace;
	private FaceShieldPoints? _previousSnapshot;
	private int _previousDamagePending;
	private bool _previousUsedThisTurn;
	private FaceShieldPoints? _previousShieldPoints;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActive = actor.ShieldBraceActive;
		_previousFace = actor.ShieldBraceFace;
		_previousSnapshot = actor.ShieldBraceSnapshot?.Clone();
		_previousDamagePending = actor.ShieldBraceDamagePending;
		_previousUsedThisTurn = actor.ShieldBraceUsedThisTurn;
		_previousShieldPoints = actor.ShieldPoints.Clone();
		ShieldBrace.Activate(actor, face);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ShieldBraceActive = _previousActive;
		actor.ShieldBraceFace = _previousFace;
		actor.ShieldBraceSnapshot = _previousSnapshot?.Clone();
		actor.ShieldBraceDamagePending = _previousDamagePending;
		actor.ShieldBraceUsedThisTurn = _previousUsedThisTurn;
		actor.ShieldPoints = _previousShieldPoints!.Clone();
	}
}
