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
		_removedIndex = actor.TemporaryShieldGrants.FindIndex(grant => grant.GrantId == grantId);
		if (_removedIndex < 0)
			return [];

		_removedGrant = actor.TemporaryShieldGrants[_removedIndex].Clone();
		actor.TemporaryShieldGrants.RemoveAt(_removedIndex);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (_removedGrant is null || _removedIndex < 0)
			return;

		var actor = world.StateOf(actorId);
		var index = System.Math.Min(_removedIndex, actor.TemporaryShieldGrants.Count);
		actor.TemporaryShieldGrants.Insert(index, _removedGrant);
		_removedGrant = null;
		_removedIndex = -1;
	}
}
