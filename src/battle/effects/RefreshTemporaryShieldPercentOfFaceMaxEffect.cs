using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class RefreshTemporaryShieldPercentOfFaceMaxEffect(string grantId, int percentOfFaceMax)
	: IEffect<BattleWorld, ActorRuntime>
{
	private TemporaryShieldGrant? _previous;
	private int _previousIndex;
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		var grants = TemporaryShields.ExtractGrants(actor);
		_previousIndex = grants.ToList().FindIndex(grant => grant.GrantId == grantId);
		_previous = _previousIndex >= 0 ? grants[_previousIndex] : null;
		_applied = false;
		TemporaryShields.TryRevokeGrant(actor, grantId, out _);

		var largestShield = actor.Loadout.BaseShieldPoints.MaxOnAnyFace;
		var largestGrant = System.Math.Round(largestShield * percentOfFaceMax / 100m, MidpointRounding.AwayFromZero);
		if (largestGrant > 0)
		{
			TemporaryShields.GrantPerFacePercentOfMax(actor, grantId, percentOfFaceMax);
			if (_previousIndex >= 0)
			{
				TemporaryShields.TryRevokeGrant(actor, grantId, out var refreshed);
				if (refreshed is null)
					throw new InvalidOperationException($"Temporary shield grant '{grantId}' was not created.");
				TemporaryShields.InsertGrant(actor, _previousIndex, refreshed);
			}
		}
		else if (_previous is null)
		{
			return [];
		}

		_applied = true;
		return [new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(actor))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		var actor = world.StateOf(actorId);
		TemporaryShields.TryRevokeGrant(actor, grantId, out _);
		if (_previous is not null)
			TemporaryShields.InsertGrant(actor, _previousIndex, _previous);
		_applied = false;
	}
}
