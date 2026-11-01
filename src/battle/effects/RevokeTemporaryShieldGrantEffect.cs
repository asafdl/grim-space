using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RevokeTemporaryShieldGrantEffect(string grantId) : IEffect<BattleWorld, ActorRuntime>
{
	private TemporaryShieldGrant? _removedGrant;
	private int _removedIndex = -1;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_removedIndex = TemporaryShields.ExtractGrants(actor).ToList().FindIndex(grant => grant.GrantId == grantId);
		if (_removedIndex < 0)
			return [];

		if (!TemporaryShields.TryRevokeGrant(actor, grantId, out _removedGrant))
		{
			_removedIndex = -1;
			return [];
		}

		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_removedGrant is null || _removedIndex < 0)
			return;

		TemporaryShields.InsertGrant(world.StateOf(actorId), _removedIndex, _removedGrant);
		_removedGrant = null;
		_removedIndex = -1;
	}
}
