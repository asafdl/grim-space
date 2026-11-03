using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Loadouts.Defenses;

namespace GrimSpace.Battle.Effects;

public sealed class ApChangeEffect(int delta) : IEffect<BattleWorld, ActorRuntime>
{
	private FaceShieldPoints? _previousShields;
	private int _previousGrantedPerFace;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.CurrentActionPoints += delta;
		if (delta >= 0 || !BattleStatuses.HasShieldDynamo(actor))
			return [];

		_previousShields = actor.CurrentShieldPoints.Clone();
		_previousGrantedPerFace = actor.ShieldDynamoGrantedPerFace;
		var perFace = -delta * PilotSkillPercents.ShieldDynamo.ShieldsPerFacePerAp;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			actor.CurrentShieldPoints[face] += perFace;
		actor.ShieldDynamoGrantedPerFace += perFace;
		return [new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(actor))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.CurrentActionPoints -= delta;
		if (_previousShields is null)
			return;

		actor.CurrentShieldPoints = _previousShields;
		actor.ShieldDynamoGrantedPerFace = _previousGrantedPerFace;
		_previousShields = null;
	}
}
