using Godot;
using GrimSpace.Battle.Presentation.Camera;

namespace GrimSpace.Tests.Presentation;

public sealed class CameraDirectorTests
{
	[Fact]
	public void ReportInterestWhenVisibleDoesNothing()
	{
		var rig = new FakeCameraRig { VisibleAtPivotOverride = (_, _) => true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		director.ReportInterest(new CameraInterest([new Vector3(100f, 0f, 0f)], CameraImportance.Combat));

		Assert.Empty(rig.TweenCalls);
	}

	[Fact]
	public void ReportInterestWhenOffScreenStartsTween()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		var offScreen = new Vector3(100f, 0f, 0f);
		director.ReportInterest(new CameraInterest([offScreen], CameraImportance.Combat));

		Assert.Single(rig.TweenCalls);
		Assert.Equal(offScreen, rig.TweenCalls[0].Target);
		Assert.Equal(BattleCameraDirector.CombatInterestTween, rig.TweenCalls[0].Duration);
	}

	[Fact]
	public void ReportInterestCoalescesWhenAutomationTargetAlreadyCoversInterest()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		var firstTarget = new Vector3(50f, 0f, 0f);
		rig.TweenPivotTo(firstTarget, 0.5f);

		rig.VisibleAtPivotOverride = (points, _) =>
			points.All(p => (p - firstTarget).Length() <= 30f);

		var nearby = new Vector3(55f, 0f, 0f);
		director.ReportInterest(new CameraInterest([nearby], CameraImportance.Combat));

		Assert.Single(rig.TweenCalls);
	}

	[Fact]
	public void ReturnControlWhenVisibleDoesNothing()
	{
		var rig = new FakeCameraRig { VisibleAtPivotOverride = (_, _) => true };
		var director = new BattleCameraDirector(rig);

		director.ReturnControl(new Vector3(5f, 0f, 0f));

		Assert.Empty(rig.TweenCalls);
	}

	[Fact]
	public void ReturnControlWhenOffScreenStartsTween()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);

		var player = new Vector3(100f, 0f, 0f);
		director.ReturnControl(player);

		Assert.Single(rig.TweenCalls);
		Assert.Equal(player, rig.TweenCalls[0].Target);
	}

	[Fact]
	public void OnManualInputStartedCancelsAutomation()
	{
		var rig = new FakeCameraRig();
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		rig.TweenPivotTo(new Vector3(10f, 0f, 0f), 0.5f);

		director.OnManualInputStarted();

		Assert.True(rig.CancelCalls > 0);
		Assert.False(rig.IsAutomationActive);
	}

	[Fact]
	public void TickResumesSoftFollowAfterGraceExpires()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.OnManualInputStarted();

		var offScreen = new Vector3(100f, 0f, 0f);
		director.Tick(BattleCameraDirector.ManualInputGrace + 0.1f, offScreen);

		Assert.NotEmpty(rig.MoveCalls);
	}

	[Fact]
	public void TickSkipsSoftFollowDuringGrace()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.OnManualInputStarted();

		director.Tick(0.1f, new Vector3(100f, 0f, 0f));

		Assert.Empty(rig.MoveCalls);
	}

	[Fact]
	public void TickSkipsSoftFollowWhileAutomationActive()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		rig.TweenPivotTo(new Vector3(50f, 0f, 0f), 0.5f);

		director.Tick(0.1f, new Vector3(100f, 0f, 0f));

		Assert.Empty(rig.MoveCalls);
	}

	[Fact]
	public void FocusPlayerRequestsPivotTween()
	{
		var rig = new FakeCameraRig();
		rig.SetPivot(Vector3.Zero);
		var director = new BattleCameraDirector(rig);

		var player = new Vector3(12f, 0f, 0f);
		director.FocusPlayer(player);

		Assert.Single(rig.TweenCalls);
		Assert.Equal(BattleCameraDirector.ManualFocusTween, rig.TweenCalls[0].Duration);
	}
}
