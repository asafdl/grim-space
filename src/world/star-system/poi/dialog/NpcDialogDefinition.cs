using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Poi.Dialog;

public sealed record NpcDialogChoice(string Id, string Label);

public sealed record NpcDialogDefinition(
	string SpeakerName,
	string Line,
	IReadOnlyList<NpcDialogChoice> Choices);

public static class FacilityNpcDialogs
{
	public const string LeaveChoiceId = "leave";
	public const string MoreChoiceId = "more";

	public static NpcDialogDefinition Idle(
		Facility facility,
		FacilityOperator facilityOperator,
		int mapSeed,
		int timelineTick,
		int rollIndex)
	{
		var random = new StableRandom(
			StableSeedMixer.From(mapSeed)
				.Add(timelineTick)
				.Add(facility.Id)
				.Add(facilityOperator.Name)
				.Add(rollIndex)
				.Value);
		var line = FacilityIdleDialogLines.Pick(facility.PresentationAnchor, random);
		return new NpcDialogDefinition(
			facilityOperator.Name,
			line,
			[
				new NpcDialogChoice(LeaveChoiceId, "Walk away"),
			]);
	}
}
