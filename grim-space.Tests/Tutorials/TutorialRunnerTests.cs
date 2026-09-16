using GrimSpace.Education;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.Tutorials;

public sealed class TutorialRunnerTests(StarMapFixture maps)
{
	[Fact]
	public void Start_UsesEducationToolsAndCompletesWhenAccepted()
	{
		var progress = new TutorialProgress();
		var dialog = new TestDialog();
		var focus = new TestWorldFocus();
		var indicator = new TestWorldIndicator();
		using var runner = new TutorialRunner(progress, dialog, focus, indicator);
		var flow = FirstContractTutorial.Create(maps.Template(42));

		var result = runner.Start(flow);
		var step = Assert.Single(flow.Steps);

		Assert.IsType<TutorialStartResult.Started>(result);
		Assert.Equal(step.TargetId, focus.FocusedObjectId);
		Assert.Equal(step.TargetId, indicator.IndicatedObjectId);
		Assert.Equal(step.Dialog, dialog.Content);
		Assert.False(progress.IsCompleted(flow.Id));

		dialog.Accept();

		Assert.True(progress.IsCompleted(flow.Id));
		Assert.Null(runner.ActiveFlow);
		Assert.False(dialog.IsOpen);
		Assert.True(indicator.Handle.IsDisposed);
		Assert.True(focus.Handle.IsDisposed);
	}

	[Fact]
	public void Start_DoesNotRepeatCompletedFlow()
	{
		var progress = new TutorialProgress();
		var flow = FirstContractTutorial.Create(maps.Template(42));
		progress.Complete(flow.Id);
		var dialog = new TestDialog();
		using var runner = new TutorialRunner(
			progress,
			dialog,
			new TestWorldFocus(),
			new TestWorldIndicator());

		var result = runner.Start(flow);

		Assert.IsType<TutorialStartResult.AlreadyCompleted>(result);
		Assert.False(dialog.IsOpen);
	}

	[Fact]
	public void WorldLinkClick_FocusesAndIndicatesLinkedObject()
	{
		var dialog = new TestDialog();
		var focus = new TestWorldFocus();
		var indicator = new TestWorldIndicator();
		using var runner = new TutorialRunner(
			new TutorialProgress(),
			dialog,
			focus,
			indicator);
		var flow = FirstContractTutorial.Create(maps.Template(42));
		runner.Start(flow);
		var initialHandle = indicator.Handle;
		var initialFocusHandle = focus.Handle;

		dialog.ClickWorldLink("pirate-1");

		Assert.Equal("pirate-1", focus.FocusedObjectId);
		Assert.Equal("pirate-1", indicator.IndicatedObjectId);
		Assert.True(initialHandle.IsDisposed);
		Assert.False(initialFocusHandle.IsDisposed);
		Assert.False(indicator.Handle.IsDisposed);

		runner.Dispose();

		Assert.True(initialFocusHandle.IsDisposed);
		Assert.True(focus.Handle.IsDisposed);
	}

	[Fact]
	public void Accept_AdvancesAcrossTargetlessAndTargetedStepsBeforeCompleting()
	{
		var progress = new TutorialProgress();
		var dialog = new TestDialog();
		var focus = new TestWorldFocus();
		var indicator = new TestWorldIndicator();
		using var runner = new TutorialRunner(progress, dialog, focus, indicator);
		var introduction = new TutorialStep(
			null,
			new TutorialDialogContent("Battle basics", "Review the battlefield."));
		var target = new TutorialStep(
			"enemy-1",
			new TutorialDialogContent("Enemy ship", "Select the enemy ship."));
		var flow = new TutorialFlow("battle-basics", [introduction, target]);

		runner.Start(flow);

		Assert.Equal(introduction, runner.ActiveStep);
		Assert.Null(focus.FocusedObjectId);
		Assert.Null(indicator.IndicatedObjectId);

		dialog.Accept();

		Assert.Equal(target, runner.ActiveStep);
		Assert.Equal(target.TargetId, focus.FocusedObjectId);
		Assert.Equal(target.TargetId, indicator.IndicatedObjectId);
		Assert.Equal(target.Dialog, dialog.Content);
		Assert.False(progress.IsCompleted(flow.Id));

		dialog.Accept();

		Assert.Null(runner.ActiveFlow);
		Assert.Null(runner.ActiveStep);
		Assert.True(progress.IsCompleted(flow.Id));
		Assert.True(indicator.Handle.IsDisposed);
	}

	[Fact]
	public void Start_CanFocusWithoutShowingIndicator()
	{
		var dialog = new TestDialog();
		var focus = new TestWorldFocus();
		var indicator = new TestWorldIndicator();
		using var runner = new TutorialRunner(
			new TutorialProgress(),
			dialog,
			focus,
			indicator);
		var step = new TutorialStep(
			"battle-overview",
			new TutorialDialogContent("Battle overview", "Review the battlefield."),
			ShowIndicator: false);

		var result = runner.Start(new TutorialFlow("battle-basics", [step]));

		Assert.IsType<TutorialStartResult.Started>(result);
		Assert.Equal(step.TargetId, focus.FocusedObjectId);
		Assert.Null(indicator.IndicatedObjectId);
		Assert.Equal(step.Dialog, dialog.Content);
	}

	[Fact]
	public void Accept_RestoresFocusWhenStepDoesNotRetainIt()
	{
		var dialog = new TestDialog();
		var focus = new TestWorldFocus();
		using var runner = new TutorialRunner(
			new TutorialProgress(),
			dialog,
			focus,
			new TestWorldIndicator());
		var focused = new TutorialStep(
			"enemy-1",
			new TutorialDialogContent("Enemy", "Review the enemy."));
		var targetless = new TutorialStep(
			null,
			new TutorialDialogContent("Controls", "Review the controls."));
		runner.Start(new TutorialFlow("battle-basics", [focused, targetless]));
		var focusHandle = focus.Handle;

		dialog.Accept();

		Assert.True(focusHandle.IsDisposed);
		Assert.Equal(targetless, runner.ActiveStep);
	}

	[Fact]
	public void FirstContractFlow_TargetsGeneratedAdministrativeCore()
	{
		var map = maps.Template(42);

		var flow = FirstContractTutorial.Create(map);
		var step = Assert.Single(flow.Steps);

		Assert.Equal(map.Blueprint.SupplyPlan.AdministrativePoiId, step.TargetId);
		Assert.Contains("Right-click", step.Dialog.Message);
		Assert.Contains("Administrative Core", step.Dialog.Message);
		Assert.Contains(
			$"[url={map.Blueprint.SupplyPlan.AdministrativePoiId}]Administrative Core[/url]",
			step.Dialog.Message);
		Assert.Equal("Accept", step.Dialog.AcceptText);
	}

	[Fact]
	public void FirstBattleFlow_EndsWithMovementAndFlakTraining()
	{
		var flow = FirstBattleTutorial.Create();

		Assert.Equal(FirstBattleTutorial.Id, flow.Id);
		Assert.True(BattleController.ShouldReturnToPlayerAfterTutorialStep(
			flow,
			flow.Steps[3]));
		Assert.False(BattleController.ShouldReturnToPlayerAfterTutorialStep(
			flow,
			flow.Steps[4]));
		Assert.Collection(
			flow.Steps,
			overview =>
			{
				Assert.Equal(FirstBattleTutorial.OverviewTargetId, overview.TargetId);
				Assert.False(overview.ShowIndicator);
				Assert.True(overview.FocusTarget);
				Assert.True(overview.RetainFocusAfterStep);
				Assert.Equal("Continue", overview.Dialog.AcceptText);
			},
			enemies =>
			{
				Assert.Equal(FirstBattleTutorial.EnemiesTargetId, enemies.TargetId);
				Assert.True(enemies.ShowIndicator);
				Assert.False(enemies.FocusTarget);
			},
			inspection =>
			{
				Assert.Null(inspection.TargetId);
				Assert.Contains("movement bubble", inspection.Dialog.Message);
				Assert.Equal("Continue", inspection.Dialog.AcceptText);
			},
			player =>
			{
				Assert.Equal(FirstBattleTutorial.PlayerTargetId, player.TargetId);
				Assert.True(player.ShowIndicator);
				Assert.False(player.FocusTarget);
				Assert.Equal("Begin Battle", player.Dialog.AcceptText);
			},
			movement =>
			{
				Assert.Equal(FirstBattleTutorial.PlayerTargetId, movement.TargetId);
				Assert.False(movement.ShowIndicator);
				Assert.True(movement.FocusTarget);
				Assert.True(movement.RetainFocusAfterStep);
			},
			plan =>
			{
				Assert.Equal(FirstBattleTutorial.PlanMovementTargetId, plan.TargetId);
				Assert.False(plan.FocusTarget);
				Assert.False(plan.AdvanceOnAccept);
				Assert.Null(plan.Dialog.AcceptText);
			},
			undo =>
			{
				Assert.Equal(FirstBattleTutorial.UndoTargetId, undo.TargetId);
				Assert.False(undo.FocusTarget);
				Assert.False(undo.AdvanceOnAccept);
				Assert.Null(undo.Dialog.AcceptText);
			},
			weapons =>
			{
				Assert.Equal(FirstBattleTutorial.WeaponTrainingTargetId, weapons.TargetId);
				Assert.False(weapons.ShowIndicator);
				Assert.True(weapons.FocusTarget);
				Assert.True(weapons.RetainFocusAfterStep);
			},
			flak =>
			{
				Assert.Equal(FirstBattleTutorial.QueueFlakTargetId, flak.TargetId);
				Assert.False(flak.FocusTarget);
				Assert.False(flak.AdvanceOnAccept);
				Assert.Null(flak.Dialog.AcceptText);
			},
			undoFlak =>
			{
				Assert.Equal(FirstBattleTutorial.UndoFlakTargetId, undoFlak.TargetId);
				Assert.False(undoFlak.FocusTarget);
				Assert.False(undoFlak.AdvanceOnAccept);
				Assert.Null(undoFlak.Dialog.AcceptText);
			},
			camera =>
			{
				Assert.Null(camera.TargetId);
				Assert.False(camera.ShowIndicator);
				Assert.False(camera.FocusTarget);
				Assert.True(camera.AdvanceOnAccept);
				Assert.Equal("Continue", camera.Dialog.AcceptText);
			},
			sendOff =>
			{
				Assert.Null(sendOff.TargetId);
				Assert.Equal("End Tutorial", sendOff.Dialog.Title);
				Assert.Equal("Begin Battle", sendOff.Dialog.AcceptText);
			});
	}

	private sealed class TestDialog : ITutorialDialog
	{
		public event Action? Accepted;

		public event Action? AssistanceRequested;

		public event Action<string>? WorldLinkClicked;

		public bool IsOpen { get; private set; }

		public TutorialDialogContent? Content { get; private set; }

		public TutorialAssistanceContent? Assistance { get; private set; }

		public void Open(TutorialDialogContent content)
		{
			Content = content;
			Assistance = null;
			IsOpen = true;
		}

		public void ShowAssistance(TutorialAssistanceContent content) => Assistance = content;

		public void ClearAssistance() => Assistance = null;

		public void Close()
		{
			Assistance = null;
			IsOpen = false;
		}

		public void Accept() => Accepted?.Invoke();

		public void RequestAssistance() => AssistanceRequested?.Invoke();

		public void ClickWorldLink(string objectId) => WorldLinkClicked?.Invoke(objectId);
	}

	private sealed class TestWorldFocus : IWorldFocus
	{
		public TestFocusHandle Handle { get; private set; } = null!;

		public string? FocusedObjectId { get; private set; }

		public WorldFocusResult Focus(string objectId)
		{
			FocusedObjectId = objectId;
			Handle = new TestFocusHandle();
			return new WorldFocusResult.Accepted(Handle);
		}
	}

	private sealed class TestFocusHandle : IWorldFocusHandle
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}

	private sealed class TestWorldIndicator : IWorldIndicator
	{
		public TestIndicatorHandle Handle { get; private set; } = null!;

		public string? IndicatedObjectId { get; private set; }

		public WorldIndicatorResult Show(string objectId)
		{
			IndicatedObjectId = objectId;
			Handle = new TestIndicatorHandle();
			return new WorldIndicatorResult.Shown(Handle);
		}
	}

	private sealed class TestIndicatorHandle : IWorldIndicatorHandle
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}
}
