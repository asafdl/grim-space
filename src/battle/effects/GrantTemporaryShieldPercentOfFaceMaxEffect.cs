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
		var actor = world.StateOf(actorId);
		TemporaryShields.GrantPerFacePercentOfMax(actor, grantId, percentOfFaceMax);
		_applied = true;
		return [new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(actor))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		TemporaryShields.TryRevokeGrant(world.StateOf(actorId), grantId, out _);
		_applied = false;
	}
}
