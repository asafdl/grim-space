using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Math.Camera;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class BattleCameraDirectorTests
{
	[Fact]
	public void EnemySequence_MovesOnceToOverviewThenHolds()
	{
		var rig = new Rig { HasOverview = true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		Vector3[] points = [Vector3.Zero, new Vector3(20, 0, 0)];

		var cut = director.PrepareEnemyOverview(points);
		Assert.Equal(points, rig.OverviewPoints);
		Assert.NotNull(cut);
		Assert.Null(rig.SnappedPose);
		cut();
		Assert.NotNull(rig.SnappedPose);

		director.ReportInterest(new CameraInterest([points[1]], CameraImportance.Combat));
		director.Tick(0.1f, new Vector3(-30, 0, 0));

		Assert.Equal(1, rig.OverviewCalls);
		Assert.Empty(rig.Tweens);
		Assert.Equal(0, rig.Follows);
	}

	[Fact]
	public void EnemyOverview_CanTweenToOverviewInsteadOfCutting()
	{
		var rig = new Rig { HasOverview = true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		director.PrepareEnemyOverview([Vector3.Zero], tween: true)!.Invoke();

		Assert.Null(rig.SnappedPose);
		Assert.Equal(new Vector3(1, 0, 0), rig.TweenedFocus!.Value.Pivot);
		Assert.Equal(40, rig.TweenedFocus.Value.Distance);
		Assert.Equal(1, rig.TweenedFocus.Value.Yaw);
		Assert.Equal(0.5f, rig.TweenedFocus.Value.Pitch);
	}

	[Fact]
	public void EnemyOverview_ManualInputCancelsPendingTween()
	{
		var rig = new Rig { HasOverview = true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		var overview = director.PrepareEnemyOverview([Vector3.Zero], tween: true);

		director.OnManualInputStarted();
		overview!.Invoke();

		Assert.Null(rig.TweenedFocus);
		Assert.Null(rig.SnappedPose);
	}

	[Fact]
	public void EnemyOverview_SubstantialMovementRequestsFullTransitionAndWaitsForTween()
	{
		var rig = new Rig { HasOverview = true, CurrentPose = new OrbitPose { Distance = 20 } };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		var overview = director.PrepareEnemyOverview([Vector3.Zero], tween: true);

		Assert.True(director.EnemyOverviewNeedsFullTransition);
		overview!.Invoke();
		Assert.True(director.IsEnemyOverviewMoving);
		Assert.InRange(rig.TweenedFocusDuration, 0.9f, 1.2f);
		director.OnManualInputStarted();
		Assert.False(director.IsEnemyOverviewMoving);
	}

	[Fact]
	public void EnemyOverview_SmallMovementKeepsFastTransitionAndCuts()
	{
		var rig = new Rig
		{
			HasOverview = true,
			CurrentPose = new OrbitPose { Pivot = Vector3.Right, Distance = 39, Yaw = 1, Pitch = 0.5f },
		};
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		var overview = director.PrepareEnemyOverview([Vector3.Zero], tween: true);

		Assert.False(director.EnemyOverviewNeedsFullTransition);
		overview!.Invoke();
		Assert.NotNull(rig.SnappedPose);
		Assert.Null(rig.TweenedFocus);
	}

	[Fact]
	public void ManualInput_DoesNotRestartAutomaticEnemyFraming()
	{
		var rig = new Rig();
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.PrepareEnemyOverview([Vector3.Zero])?.Invoke();
		director.OnManualInputStarted();
		director.ReportInterest(new CameraInterest([Vector3.Right], CameraImportance.Combat));
		director.Tick(0.1f, new Vector3(-30, 0, 0));

		Assert.Equal(1, rig.OverviewCalls);
		Assert.Empty(rig.Tweens);
		Assert.Equal(0, rig.Follows);
	}

	[Fact]
	public void PlayerPlayback_StillFollowsThePlayer()
	{
		var rig = new Rig
		{
			OnScreen = false,
			Correction = new Vector3(-10, 0, 0),
		};
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();

		director.Tick(0.1f, new Vector3(-30, 0, 0));

		Assert.Equal(1, rig.Follows);
	}

	[Fact]
	public void ReplayReturn_RestoresOriginalAngleAndZoomOnlyWhenTransitionApplies()
	{
		var original = new OrbitPose { Pivot = new Vector3(1, 2, 3), Distance = 24, Yaw = 0.8f, Pitch = 0.4f };
		var rig = new Rig { CurrentPose = original, HasOverview = true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.PrepareEnemyOverview([Vector3.Zero])!.Invoke();
		rig.SnappedPose = null;
		rig.CurrentPose = new OrbitPose { Distance = 50, Yaw = -1, Pitch = -0.5f };
		director.PrepareReturnControl();

		Assert.Null(rig.SnappedPose);
		director.CompleteReturnControl(new Vector3(4, 5, 6));

		Assert.NotNull(rig.SnappedPose);
		Assert.Equal(new Vector3(4, 5, 6), rig.SnappedPose.Value.Pivot);
		Assert.Equal(original.Distance, rig.SnappedPose.Value.Distance);
		Assert.Equal(original.Yaw, rig.SnappedPose.Value.Yaw);
		Assert.Equal(original.Pitch, rig.SnappedPose.Value.Pitch);
	}

	[Fact]
	public void ReplayReturn_RespectsManualCameraChangesDuringOverview()
	{
		var rig = new Rig { HasOverview = true, OnScreen = false };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.PrepareEnemyOverview([Vector3.Zero])!.Invoke();
		rig.SnappedPose = null;
		director.OnManualInputStarted();
		director.PrepareReturnControl();

		director.CompleteReturnControl(Vector3.Right);

		Assert.Null(rig.SnappedPose);
		Assert.Empty(rig.Tweens);
	}

	[Fact]
	public void ReplayReturn_WithoutOverviewRetainsExistingOffscreenFollow()
	{
		var rig = new Rig { OnScreen = false };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.PrepareReturnControl();

		director.CompleteReturnControl(Vector3.Right);

		Assert.Null(rig.SnappedPose);
		Assert.Single(rig.Tweens);
		Assert.Equal(Vector3.Right, rig.Tweens[0].Target);
	}

	[Fact]
	public void ReplayReturn_ToNewPlayerCentersDuringTransitionWithoutChangingAngleOrZoom()
	{
		var pose = new OrbitPose { Pivot = Vector3.Left, Distance = 24, Yaw = 0.8f, Pitch = 0.4f };
		var rig = new Rig { CurrentPose = pose, OnScreen = true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.PrepareReturnControl(focusNewPlayer: true);

		Assert.Null(rig.SnappedPose);
		director.CompleteReturnControl(Vector3.Right);

		Assert.NotNull(rig.SnappedPose);
		Assert.Equal(Vector3.Right, rig.SnappedPose.Value.Pivot);
		Assert.Equal(pose.Distance, rig.SnappedPose.Value.Distance);
		Assert.Equal(pose.Yaw, rig.SnappedPose.Value.Yaw);
		Assert.Equal(pose.Pitch, rig.SnappedPose.Value.Pitch);
		Assert.Empty(rig.Tweens);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReplayReturn_ToNewPlayerRespectsManualInput(bool afterReturnPrepared)
	{
		var rig = new Rig { OnScreen = false };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		if (afterReturnPrepared)
			director.PrepareReturnControl(focusNewPlayer: true);
		director.OnManualInputStarted();
		if (!afterReturnPrepared)
			director.PrepareReturnControl(focusNewPlayer: true);

		director.CompleteReturnControl(Vector3.Right);

		Assert.Null(rig.SnappedPose);
		Assert.Empty(rig.Tweens);
	}

	[Fact]
	public void ReplayReturn_InterruptedByNewPlaybackDoesNotRestoreStalePose()
	{
		var rig = new Rig { HasOverview = true };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		director.PrepareEnemyOverview([Vector3.Zero])!.Invoke();
		rig.SnappedPose = null;
		director.PrepareReturnControl();
		director.BeginPlayback();

		director.CompleteReturnControl(Vector3.Right);

		Assert.Null(rig.SnappedPose);
	}

	[Fact]
	public void EnemyOverview_ManualInputBeforeCaptureCancelsPendingCut()
	{
		var rig = new Rig { HasOverview = true, OnScreen = false };
		var director = new BattleCameraDirector(rig);
		director.BeginPlayback();
		var cut = director.PrepareEnemyOverview([Vector3.Zero]);

		director.OnManualInputStarted();
		cut!.Invoke();
		director.PrepareReturnControl();
		director.CompleteReturnControl(Vector3.Right);

		Assert.Null(rig.SnappedPose);
		Assert.Empty(rig.Tweens);
	}

	private sealed class Rig : ICameraRig
	{
		public Vector3 Pivot { get; set; }
		public bool IsAutomationActive { get; set; }
		public Vector3? AutomationTarget { get; set; }
		public bool OnScreen { get; set; } = true;
		public Vector3 Correction { get; set; }
		public List<(Vector3 Target, float Duration)> Tweens { get; } = [];
		public int Follows { get; private set; }
		public bool HasOverview { get; set; }
		public int OverviewCalls { get; private set; }
		public IReadOnlyList<Vector3> OverviewPoints { get; private set; } = [];
		public OrbitPose CurrentPose { get; set; }
		public OrbitPose? SnappedPose { get; set; }
		public OrbitPose? TweenedFocus { get; set; }
		public float TweenedFocusDuration { get; set; }

		public void SetFocus(Vector3 pivot, float distance, float yaw, float pitch) =>
			SnappedPose = new OrbitPose { Pivot = pivot, Distance = distance, Yaw = yaw, Pitch = pitch };

		public void TweenFocusOn(Vector3 pivot, float distance, float yaw, float pitch, float duration)
		{
			TweenedFocus = new OrbitPose { Pivot = pivot, Distance = distance, Yaw = yaw, Pitch = pitch };
			TweenedFocusDuration = duration;
			IsAutomationActive = true;
		}

		public bool AreVisible(IReadOnlyList<Vector3> points, float margin) => OnScreen;

		public bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin) =>
			AreVisible(points, margin);

		public bool TryCalculateCorrection(
			IReadOnlyList<Vector3> points,
			float margin,
			out Vector3 targetPivot)
		{
			targetPivot = Correction;
			return !AreVisible(points, margin);
		}

		public void TweenPivotTo(Vector3 target) => Tweens.Add((target, -1));

		public void TweenPivotTo(Vector3 target, float duration) => Tweens.Add((target, duration));

		public OrbitPose? CalculateOverview(IReadOnlyList<Vector3> points, IReadOnlyList<Vector3> priorityPoints)
		{
			OverviewCalls++;
			OverviewPoints = points;
			return HasOverview ? new OrbitPose { Pivot = Vector3.Right, Distance = 40, Yaw = 1, Pitch = 0.5f } : null;
		}

		public void MovePivotToward(Vector3 target, float delta, float responseTime) => Follows++;

		public void CancelAutomation()
		{
			IsAutomationActive = false;
		}

		public void PlayImpactShake(ImpactShakeScale scale)
		{
		}
	}
}
