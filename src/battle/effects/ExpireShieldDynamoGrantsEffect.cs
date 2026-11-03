using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ExpireShieldDynamoGrantsEffect : IEffect<BattleWorld, ActorRuntime>
{
	private FaceShieldPoints? _previousShields;
	private int _previousGrantedPerFace;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousShields = actor.CurrentShieldPoints.Clone();
		_previousGrantedPerFace = actor.ShieldDynamoGrantedPerFace;
		if (_previousGrantedPerFace <= 0 || !BattleStatuses.HasShieldDynamo(actor))
		{
			actor.ShieldDynamoGrantedPerFace = 0;
			return [];
		}

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			actor.CurrentShieldPoints[face] = System.Math.Max(
				0,
				actor.CurrentShieldPoints[face] - _previousGrantedPerFace);
		}

		actor.ShieldDynamoGrantedPerFace = 0;
		return [new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(actor))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		if (_previousShields is null)
			return;

		actor.CurrentShieldPoints = _previousShields;
		actor.ShieldDynamoGrantedPerFace = _previousGrantedPerFace;
		_previousShields = null;
	}
}
