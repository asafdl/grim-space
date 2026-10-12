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
	public const string TurnInChoiceId = "turn-in";
	public const string AcceptLeadChoiceId = "accept-lead";
	public const string RejectLeadChoiceId = "reject-lead";

	public static NpcDialogDefinition DeliveryTurnIn(
		FacilityOperator facilityOperator,
		string turnInDialog,
		bool isFinal) =>
		DeliveryTurnIn(facilityOperator.Name, turnInDialog, isFinal);

	public static NpcDialogDefinition DeliveryTurnIn(
		string speakerName,
		string turnInDialog,
		bool isFinal) =>
		new(
			speakerName,
			turnInDialog,
			[
				new NpcDialogChoice(TurnInChoiceId, isFinal ? "Deliver" : "Go To Next Point"),
			]);

	public static NpcDialogDefinition BlackMarketLead(
		string speakerName,
		string line) =>
		new(
			speakerName,
			line,
			[
				new NpcDialogChoice(AcceptLeadChoiceId, "Interested"),
				new NpcDialogChoice(RejectLeadChoiceId, "Decline"),
			]);

	public static NpcDialogDefinition LeadResponse(
		string speakerName,
		string line) =>
		new(
			speakerName,
			line,
			[
				new NpcDialogChoice(LeaveChoiceId, "Walk away"),
			]);

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
