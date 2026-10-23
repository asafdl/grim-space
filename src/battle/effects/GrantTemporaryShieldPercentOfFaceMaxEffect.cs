using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class GrantTemporaryShieldPercentOfFaceMaxEffect(string grantId, int percentOfFaceMax)
	: IEffect<BattleWorld, ActorRuntime>
{
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		TemporaryShields.GrantPerFacePercentOfMax(
			world.StateOf(actorId),
			grantId,
			percentOfFaceMax);
		_applied = true;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		TemporaryShields.TryRevokeGrant(world.StateOf(actorId), grantId, out _);
		_applied = false;
	}
}
