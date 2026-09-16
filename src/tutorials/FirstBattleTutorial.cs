using GrimSpace.Education;
using GrimSpace.Components;

namespace GrimSpace.Tutorials;

public static class FirstBattleTutorial
{
	public const string Id = "first-battle";
	public const string OverviewTargetId = "battle-overview";
	public const string EnemiesTargetId = "battle-enemies";
	public const string PlayerTargetId = "battle-player";
	public const string WeaponTrainingTargetId = "battle-player-weapons";
	public const string PlanMovementTargetId = "battle-plan-movement";
	public const string UndoTargetId = "battle-undo";
	public const string QueueFlakTargetId = "battle-queue-flak";
	public const string UndoFlakTargetId = "battle-undo-flak";

	public static TutorialFlow Create() =>
		new(
			Id,
			[
				new TutorialStep(
					OverviewTargetId,
					new TutorialDialogContent(
						"Your first battle",
						"Battles take place in three-dimensional space. Movement and positioning are key.",
						"Continue"),
					ShowIndicator: false,
					RetainFocusAfterStep: true),
				new TutorialStep(
					EnemiesTargetId,
					new TutorialDialogContent(
						"Enemies",
						"Destroy the pirates to win.",
						"Continue"),
					FocusTarget: false),
				new TutorialStep(
					null,
					new TutorialDialogContent(
						"Inspect enemies",
						"Click an enemy to inspect its movement bubble, shields, and abilities.",
						"Continue"),
					ShowIndicator: false,
					FocusTarget: false),
				new TutorialStep(
					PlayerTargetId,
					new TutorialDialogContent(
						"Player",
						"This is your ship. Use its movement and abilities to kill all enemies.",
						"Begin Battle"),
					FocusTarget: false),
				new TutorialStep(
					PlayerTargetId,
					new TutorialDialogContent(
						"Movement range",
						"You can move your ship anywhere inside the movement bubble.",
						"Continue"),
					ShowIndicator: false,
					RetainFocusAfterStep: true),
				new TutorialStep(
					PlanMovementTargetId,
					new TutorialDialogContent(
						"Plan a maneuver",
						"Pick a destination tile. Hold Left Click and drag to choose a heading, then scroll to roll your ship. Queue a move with at least one roll.",
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					UndoTargetId,
					new TutorialDialogContent(
						"Undo planned actions",
						$"Nothing happens during simulation phase until you click End Turn. Press {InputShortcutText.WithPrimaryModifier("Z")} now to undo your queued maneuver.",
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					WeaponTrainingTargetId,
					new TutorialDialogContent(
						"Weapon abilities",
						"Abilities such as weapons do not spend AP. Their use is limited by cooldowns.",
						"Continue"),
					ShowIndicator: false,
					RetainFocusAfterStep: true),
				new TutorialStep(
					QueueFlakTargetId,
					new TutorialDialogContent(
						"Fire the flak cannon",
						"Select Flak in the HUD or press 2, then choose either the port or starboard firing mount.",
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					UndoFlakTargetId,
					new TutorialDialogContent(
						"Predicted impact area",
						"The highlighted volume shows where the queued shot is expected to hit. Actions remain reversible during the simulation phase.\nUndo the queued flak cannon now.",
						AcceptText: null),
					ShowIndicator: false,
					FocusTarget: false,
					AdvanceOnAccept: false),
				new TutorialStep(
					null,
					new TutorialDialogContent(
						"Camera controls",
						"Three-dimensional space can be hard to navigate. \nRight-click and drag to orbit, scroll to zoom, use WASD to pan, and press F to refocus on your ship.",
						"Continue"),
					ShowIndicator: false,
					FocusTarget: false),
				new TutorialStep(
					null,
					new TutorialDialogContent(
						"End Tutorial",
						"Tutorial messages are turned off, you can turn them back on in Settings.\nGood luck, Pilot!\nClear out those filth!",
						"Begin Battle"),
					ShowIndicator: false,
					FocusTarget: false),
			]);
}
