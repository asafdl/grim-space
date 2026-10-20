using GrimSpace.Pilots;
using GrimSpace.Battle.PilotCapabilities;

namespace GrimSpace.Battle.Encounter;

public sealed record PilotBattleSnapshot(
	string PilotId,
	IReadOnlyList<PilotCapabilityGrant> CapabilityGrants,
	string? PortraitId = null)
{
	public static PilotBattleSnapshot FromSkills(
		string pilotId,
		IEnumerable<EPilotSkillId> skillIds,
		string? portraitId = null) =>
		new(
			pilotId,
			PilotCapabilityGrant.FromSkills(
				pilotId,
				skillIds,
				BattlePilotCapabilityPipelineFactory.Supports),
			portraitId);
}
