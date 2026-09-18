using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public static class FirstBattleTutorial
{
	public const string Id = "first-battle";
	public const string Turn1MoveTargetId = "battle-turn1-move";
	public const string Turn1EndTargetId = "battle-turn1-end";
	public const string Turn2MoveTargetId = "battle-turn2-move";
	public const string Turn2TorpedoTargetId = "battle-turn2-torpedo";
	public const string Turn2EndTargetId = "battle-turn2-end";

	public static TutorialFlow Create() =>
		new(
			Id,
			[
				new TutorialStep(
					Turn1MoveTargetId,
					new TutorialDialogContent(
						"Close the distance",
						TutorialCopy.MoveToGhostShip,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					Turn1EndTargetId,
					new TutorialDialogContent(
						"End simulation phase",
						TutorialCopy.EndTurnPrompt,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					Turn2MoveTargetId,
					new TutorialDialogContent(
						"Reposition",
						TutorialCopy.Turn2MovePrompt,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					Turn2TorpedoTargetId,
					new TutorialDialogContent(
						"Torpedo",
						TutorialCopy.LaunchVentralTorpedo,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					Turn2EndTargetId,
					new TutorialDialogContent(
						"Fire solution",
						TutorialCopy.EndTurnPrompt,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
			]);
}
