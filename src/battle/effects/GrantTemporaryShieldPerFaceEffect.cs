using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class GrantTemporaryShieldPerFaceEffect(string grantId, int perFace)
	: IEffect<BattleWorld, ActorRuntime>
{
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		TemporaryShields.GrantPerFace(world.StateOf(actorId), grantId, perFace);
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
