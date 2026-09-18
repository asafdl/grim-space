using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Movement;

public sealed class RouteHitPreview
{
	public MovePathSession Session { get; }

	private IReadOnlyList<PoseHitOpportunity>? _hitOpportunities;

	internal int ComputeCount { get; private set; }

	public RouteHitPreview(MovePathSession session) => Session = session;

	internal IReadOnlyList<PoseHitOpportunity> GetHitOpportunities(
		BattleSimulation sim,
		string playerId)
	{
		if (_hitOpportunities is not null)
			return _hitOpportunities;

		_hitOpportunities = ComputeHitOpportunities(sim, playerId);
		ComputeCount++;
		return _hitOpportunities;
	}

	private IReadOnlyList<PoseHitOpportunity> ComputeHitOpportunities(
		BattleSimulation sim,
		string playerId)
	{
		var fork = sim.Fork();
		foreach (var step in Session.Steps)
		{
			if (!fork.TryEnqueue(step))
			{
				throw new InvalidOperationException(
					$"Cannot replay move route step {step}.");
			}
		}

		var actor = fork.StateOf<State>(playerId);
		var runtime = fork.RuntimeFor(playerId);
		var world = fork.World;
		var specsByDef = AbilityHudCatalog.ForUnit(actor.Type).ToDictionary(spec => spec.Def);
		var opportunities = new List<PoseHitOpportunity>();

		foreach (var def in Capabilities.AbilitiesFor(actor.Type))
		{
			var spec = specsByDef[def];
			var hitTargets = new HashSet<string>(StringComparer.Ordinal);
			foreach (var action in def.Discover(world, runtime, playerId))
			{
				if (fork.Peek(action) is { } peek)
					GetImpactIds(peek.Records, hitTargets);
			}

			foreach (var targetId in hitTargets.OrderBy(id => id, StringComparer.Ordinal))
			{
				opportunities.Add(new PoseHitOpportunity(
					targetId,
					spec.IconPath,
					spec.IconTint));
			}
		}

		return opportunities;
	}

	internal static void GetImpactIds(IReadOnlyList<IRecord> records, HashSet<string> targets)
	{
		foreach (var record in records)
		{
			if (record is Record<ImpactFacts> { Value.TargetId: var targetId })
				targets.Add(targetId);
		}
	}
}
