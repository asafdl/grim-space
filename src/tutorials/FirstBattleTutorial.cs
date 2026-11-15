using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public static class FirstBattleTutorial
{
	public const string Id = "first-battle";
	public const string ShieldApproachTargetId = "battle-shield-approach";
	public const string ShieldShotTargetId = "battle-shield-shot";
	public const string UndoTargetId = "battle-undo-plan";
	public const string FindKillTargetId = "battle-find-kill";
	public const string ExecuteTargetId = "battle-execute-kill";
	public const string GoodLuckPilot = "battle-good-luck-pilot";

	public static TutorialFlow Create() =>
		new(
			Id,
			[
				new TutorialStep(
					ShieldApproachTargetId,
					new TutorialDialogContent(
						"Line up a shot",
						TutorialCopy.ShieldApproach,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					ShieldShotTargetId,
					new TutorialDialogContent(
						"Preview your attack",
						TutorialCopy.QueueShieldShot,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					UndoTargetId,
					new TutorialDialogContent(
						"Shields absorb the shot",
						TutorialCopy.UndoShieldShot,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					FindKillTargetId,
					new TutorialDialogContent(
						"Find a killing angle",
						TutorialCopy.FindKillShot,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					ExecuteTargetId,
					new TutorialDialogContent(
						"Execute your plan",
						TutorialCopy.ExecuteKillShot,
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					GoodLuckPilot,
					new TutorialDialogContent(
						"Good Luck Pilot",
						TutorialCopy.EndBattleDialog,
						AcceptText: "Accept"),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: true),
			]);
}
