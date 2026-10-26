using System.Collections.Frozen;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.Runtime;

public sealed class ActorRuntime : IRuntimeContext<ActorRuntime>
{
	private static readonly FrozenSet<EPilotCapabilityId> EmptyCapabilities =
		FrozenSet<EPilotCapabilityId>.Empty;
	private static readonly IReadOnlyList<PilotCapabilityGrant> EmptyGrants =
		Array.Empty<PilotCapabilityGrant>();

	public ActorRuntime() : this(
		null,
		null,
		EmptyGrants,
		EmptyCapabilities,
		BattlePilotCapabilityPipelineFactory.Build(EmptyCapabilities)) { }

	private ActorRuntime(
		string? pilotId,
		string? portraitId,
		IReadOnlyList<PilotCapabilityGrant> pilotCapabilityGrants,
		IReadOnlySet<EPilotCapabilityId> pilotCapabilities,
		ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> actionEffects)
	{
		PilotId = pilotId;
		PortraitId = portraitId;
		PilotCapabilityGrants = pilotCapabilityGrants;
		PilotCapabilities = pilotCapabilities;
		ActionEffects = actionEffects;
	}

	public string? PilotId { get; }

	public string? PortraitId { get; }

	public IReadOnlyList<PilotCapabilityGrant> PilotCapabilityGrants { get; }

	public IReadOnlySet<EPilotCapabilityId> PilotCapabilities { get; }

	public ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> ActionEffects { get; }

	public static ActorRuntime From(PilotBattleSnapshot? snapshot)
	{
		if (snapshot is null)
			return new ActorRuntime(
				null,
				null,
				EmptyGrants,
				EmptyCapabilities,
				BattlePilotCapabilityPipelineFactory.Build(EmptyCapabilities));

		var grants = snapshot.CapabilityGrants.Count == 0
			? EmptyGrants
			: snapshot.CapabilityGrants.ToArray();
		var capabilities = grants.Count == 0
			? EmptyCapabilities
			: grants
				.Select(grant => grant.CapabilityId)
				.ToFrozenSet();
		return new ActorRuntime(
			snapshot.PilotId,
			snapshot.PortraitId,
			grants,
			capabilities,
			BattlePilotCapabilityPipelineFactory.Build(capabilities));
	}

	public void Reset() { }

	public ActorRuntime Fork() => this;
}
