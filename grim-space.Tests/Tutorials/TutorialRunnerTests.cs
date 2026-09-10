using GrimSpace.Education;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.Tutorials;

public sealed class TutorialRunnerTests(DevStarMapFixture maps)
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

		Assert.IsType<TutorialStartResult.Started>(result);
		Assert.Equal(flow.WorldObjectId, focus.FocusedObjectId);
		Assert.Equal(flow.WorldObjectId, indicator.IndicatedObjectId);
		Assert.Equal(flow.Dialog, dialog.Content);
		Assert.False(progress.IsCompleted(flow.Id));

		dialog.Accept();

		Assert.True(progress.IsCompleted(flow.Id));
		Assert.Null(runner.ActiveFlow);
		Assert.False(dialog.IsOpen);
		Assert.True(indicator.Handle.IsDisposed);
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
	public void FirstContractFlow_TargetsGeneratedAdministrativeCore()
	{
		var map = maps.Template(42);

		var flow = FirstContractTutorial.Create(map);

		Assert.Equal(map.Blueprint.SupplyPlan.AdministrativePoiId, flow.WorldObjectId);
		Assert.Contains("Right-click", flow.Dialog.Message);
		Assert.Contains("Administrative Core", flow.Dialog.Message);
		Assert.Equal("Accept", flow.Dialog.AcceptText);
	}

	private sealed class TestDialog : ITutorialDialog
	{
		public event Action? Accepted;

		public bool IsOpen { get; private set; }

		public TutorialDialogContent? Content { get; private set; }

		public void Open(TutorialDialogContent content)
		{
			Content = content;
			IsOpen = true;
		}

		public void Close() => IsOpen = false;

		public void Accept() => Accepted?.Invoke();
	}

	private sealed class TestWorldFocus : IWorldFocus
	{
		public string? FocusedObjectId { get; private set; }

		public WorldFocusResult Focus(string objectId)
		{
			FocusedObjectId = objectId;
			return new WorldFocusResult.Accepted();
		}
	}

	private sealed class TestWorldIndicator : IWorldIndicator
	{
		public TestIndicatorHandle Handle { get; } = new();

		public string? IndicatedObjectId { get; private set; }

		public WorldIndicatorResult Show(string objectId)
		{
			IndicatedObjectId = objectId;
			return new WorldIndicatorResult.Shown(Handle);
		}
	}

	private sealed class TestIndicatorHandle : IWorldIndicatorHandle
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}
}
