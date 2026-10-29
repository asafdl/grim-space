using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ReleaseShieldBraceEffect : IEffect<BattleWorld, ActorRuntime>
{
	private bool _previousActive;
	private ESpatialOrientation? _previousFace;
	private FaceShieldPoints? _previousSnapshot;
	private int _previousDamagePending;
	private FaceShieldPoints? _previousShieldPoints;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousActive = actor.ShieldBraceActive;
		_previousFace = actor.ShieldBraceFace;
		_previousSnapshot = actor.ShieldBraceSnapshot?.Clone();
		_previousDamagePending = actor.ShieldBraceDamagePending;
		_previousShieldPoints = actor.ShieldPoints.Clone();
		ShieldBrace.Release(actor);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ShieldBraceActive = _previousActive;
		actor.ShieldBraceFace = _previousFace;
		actor.ShieldBraceSnapshot = _previousSnapshot?.Clone();
		actor.ShieldBraceDamagePending = _previousDamagePending;
		actor.ShieldPoints = _previousShieldPoints!.Clone();
	}
}
