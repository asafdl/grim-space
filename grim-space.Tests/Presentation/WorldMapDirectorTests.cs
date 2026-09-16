using Godot;
using GrimSpace.Math.Camera;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class WorldMapDirectorTests
{
	[Fact]
	public void TryExitCinematicReturnsExitTargetMissing()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		var result = director.TryExit("cinematic");

		Assert.False(result.Succeeded);
		Assert.Equal(PresentationTransitionFailure.ExitTargetMissing, result.Failure);
	}

	[Fact]
	public void TryEnterOverviewFromCinematicStartsTransition()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		var result = director.TryEnter("overview");

		Assert.True(result.Succeeded);
		Assert.True(director.IsTransitioning);
		Assert.Equal("cinematic", director.CurrentModeId);
		Assert.Contains("Exiting:cinematic->overview", harness.Cinematic.Lifecycle);
	}

	[Fact]
	public void CompletingTransitionSetsOverviewMode()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();

		Assert.False(director.IsTransitioning);
		Assert.Equal("overview", director.CurrentModeId);
		Assert.Contains("Settled:overview", harness.Overview.Lifecycle);
	}

	[Fact]
	public void TryExitOverviewReturnsToCinematic()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();

		var result = director.TryExit("overview");

		Assert.True(result.Succeeded);
		harness.CompleteTween();
		Assert.Equal("cinematic", director.CurrentModeId);
	}

	[Fact]
	public void OverviewCannotEnterFacade()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();

		var result = director.TryEnter(
			"facade",
			new FacadeEnterPayload("poi-a"));

		Assert.False(result.Succeeded);
		Assert.Equal(PresentationTransitionFailure.NotAllowed, result.Failure);
	}

	[Fact]
	public void FacadeRequiresValidPayload()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		var missingPayload = director.TryEnter("facade");
		var wrongPayload = director.TryEnter("facade", "bad");

		Assert.False(missingPayload.Succeeded);
		Assert.Equal(PresentationTransitionFailure.InvalidPayload, missingPayload.Failure);
		Assert.False(wrongPayload.Succeeded);
		Assert.Equal(PresentationTransitionFailure.InvalidPayload, wrongPayload.Failure);
	}

	[Fact]
	public void FacadeEnterRejectsUndockedPoi()
	{
		var harness = new TestPresentationHarness { DockedPoiId = null };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		var result = director.TryEnter("facade", new FacadeEnterPayload("poi-a"));

		Assert.False(result.Succeeded);
		Assert.Equal(PresentationTransitionFailure.CanEnterRejected, result.Failure);
	}

	[Fact]
	public void FacadeEnterAcceptsDockedPoi()
	{
		var harness = new TestPresentationHarness { DockedPoiId = "poi-a" };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		var result = director.TryEnter("facade", new FacadeEnterPayload("poi-a"));

		Assert.True(result.Succeeded);
	}

	[Fact]
	public void SetInitialModeFacadeRunsLifecycleWithoutCanEnter()
	{
		var harness = new TestPresentationHarness { DockedPoiId = null };
		var director = harness.CreateDirector();

		director.SetInitialMode("facade", new FacadeEnterPayload("poi-a"));

		Assert.Equal("facade", director.CurrentModeId);
		Assert.Contains("Entering:facade", harness.Facade.Lifecycle);
		Assert.Contains("Settled:facade", harness.Facade.Lifecycle);
		Assert.Contains("Snap", harness.Calls.Select(call => call.Kind));
	}

	[Fact]
	public void SetInitialModeFacadeRejectsInvalidPayload()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();

		Assert.Throws<InvalidOperationException>(() => director.SetInitialMode("facade"));
	}

	[Fact]
	public void ModeBusyBlocksTransitions()
	{
		var harness = new TestPresentationHarness { DockedPoiId = "poi-a" };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("facade", new FacadeEnterPayload("poi-a"));
		harness.CompleteTween();
		harness.Facade.IsBusy = true;

		var exit = director.TryExit("facade");
		var enter = director.TryEnter("overview");

		Assert.False(exit.Succeeded);
		Assert.Equal(PresentationTransitionFailure.ModeBusy, exit.Failure);
		Assert.False(enter.Succeeded);
		Assert.Equal(PresentationTransitionFailure.ModeBusy, enter.Failure);
	}

	[Fact]
	public void TransitioningBlocksSecondTryEnter()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");

		var second = director.TryEnter("facade", new FacadeEnterPayload("poi-a"));

		Assert.True(director.IsTransitioning);
		Assert.False(second.Succeeded);
		Assert.Equal(PresentationTransitionFailure.AlreadyTransitioning, second.Failure);
	}

	[Fact]
	public void PrepareForFocusFromCinematicEntersOverviewBeforeCallback()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		var ran = false;

		var result = director.PrepareForFocus(() => ran = true);

		Assert.True(result.Succeeded);
		Assert.False(ran);
		Assert.True(director.IsTransitioning);
		harness.CompleteTween();
		Assert.True(ran);
		Assert.Equal("overview", director.CurrentModeId);
	}

	[Fact]
	public void PrepareForFocusFromOverviewRunsImmediately()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();
		var ran = false;

		var result = director.PrepareForFocus(() => ran = true);

		Assert.True(result.Succeeded);
		Assert.True(ran);
		Assert.Equal("overview", director.CurrentModeId);
	}

	[Fact]
	public void PrepareForFocusFromFacadeReturnsToOverviewBeforeCallback()
	{
		var harness = new TestPresentationHarness { DockedPoiId = "poi-a" };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("facade", new FacadeEnterPayload("poi-a"));
		harness.CompleteTween();
		var ran = false;

		var result = director.PrepareForFocus(() => ran = true);

		Assert.True(result.Succeeded);
		Assert.False(ran);
		harness.CompleteTween();
		Assert.False(ran);
		Assert.Equal("cinematic", director.CurrentModeId);
		harness.CompleteTween();
		Assert.True(ran);
		Assert.Equal("overview", director.CurrentModeId);
	}

	[Fact]
	public void PrepareForFocusReturnsUnavailableWhileBusy()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		harness.Cinematic.IsBusy = true;

		var result = director.PrepareForFocus(() => { });

		Assert.False(result.Succeeded);
		Assert.Equal(PresentationTransitionFailure.ModeBusy, result.Failure);
	}

	[Fact]
	public void EffectiveInputPolicyLocksDuringTransition()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");

		Assert.Equal(PresentationInputPolicy.Locked, director.EffectiveInputPolicy);
	}

	[Fact]
	public void OnPlayerMovementFromOverviewEntersCinematic()
	{
		var harness = new TestPresentationHarness { TravelActive = true };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();

		var result = director.OnPlayerMovement();

		Assert.True(result.Succeeded);
		Assert.True(director.IsTransitioning);
		harness.CompleteTween();
		Assert.Equal("cinematic", director.CurrentModeId);
	}

	[Fact]
	public void OnPlayerMovementFromOverviewIgnoresWhenNotTraveling()
	{
		var harness = new TestPresentationHarness { TravelActive = false };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();

		var result = director.OnPlayerMovement();

		Assert.True(result.Succeeded);
		Assert.False(director.IsTransitioning);
		Assert.Equal("overview", director.CurrentModeId);
	}

	[Fact]
	public void OnPlayerMovementFromCinematicDoesNotSnap()
	{
		var harness = new TestPresentationHarness { TravelActive = true };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		harness.Calls.Clear();

		var result = director.OnPlayerMovement();

		Assert.True(result.Succeeded);
		Assert.Equal("cinematic", director.CurrentModeId);
		Assert.DoesNotContain("Snap", harness.Calls.Select(call => call.Kind));
	}

	[Fact]
	public void DeferredMovementReQueriesTravelState()
	{
		var harness = new TestPresentationHarness { TravelActive = true };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");

		var result = director.OnPlayerMovement();

		Assert.True(result.Succeeded);
		Assert.True(director.IsTransitioning);
		harness.CompleteTween();
		director.Update(0);
		Assert.True(director.IsTransitioning);
	}

	[Fact]
	public void StaleDeferredMovementDoesNotChangeModes()
	{
		var harness = new TestPresentationHarness { TravelActive = true };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		director.OnPlayerMovement();
		harness.CompleteTween();
		harness.TravelActive = false;

		director.Update(0);

		Assert.Equal("overview", director.CurrentModeId);
		Assert.False(director.IsTransitioning);
	}

	[Fact]
	public void BeginTransitionDoesNotApplyLimitsBeforeTween()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		harness.Calls.Clear();

		director.TryEnter("overview");

		Assert.DoesNotContain("ApplyLimits", harness.Calls.Select(call => call.Kind));
		Assert.Contains("Tween", harness.Calls.Select(call => call.Kind));
	}

	[Fact]
	public void UpdateSkipsOutgoingModeWhileTransitioning()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		harness.Cinematic.UpdateCallCount = 0;

		director.TryEnter("overview");
		director.Update(0.1);

		Assert.Equal(0, harness.Cinematic.UpdateCallCount);

		harness.CompleteTween();
		harness.Overview.UpdateCallCount = 0;
		director.Update(0.1);

		Assert.Equal(1, harness.Overview.UpdateCallCount);
	}

	[Fact]
	public void CameraOcclusionTracksTransitionTarget()
	{
		var harness = new TestPresentationHarness();
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		Assert.True(harness.CameraOcclusionEnabled);

		director.TryEnter("overview");

		Assert.False(harness.CameraOcclusionEnabled);

		harness.CompleteTween();
		director.TryExit("overview");

		Assert.True(harness.CameraOcclusionEnabled);
	}

	private sealed class TestPresentationHarness
	{
		public StubPresentationMode Cinematic { get; } = new()
		{
			Id = "cinematic",
			ExitTargetId = null,
			AllowedFrom = new HashSet<string>(StringComparer.Ordinal) { "overview", "facade" },
			InputPolicy = new PresentationInputPolicy(true, true, true, true, true),
		};

		public StubPresentationMode Overview { get; } = new()
		{
			Id = "overview",
			ExitTargetId = "cinematic",
			AllowedFrom = new HashSet<string>(StringComparer.Ordinal) { "cinematic" },
			InputPolicy = new PresentationInputPolicy(true, true, true, true, true),
		};

		public StubPresentationMode Facade { get; } = new()
		{
			Id = "facade",
			ExitTargetId = "cinematic",
			AllowedFrom = new HashSet<string>(StringComparer.Ordinal) { "cinematic" },
			RequiresFacadePayload = true,
			InputPolicy = new PresentationInputPolicy(true, false, false, false, false),
		};

		public string? DockedPoiId { get; init; } = "poi-a";
		public bool CanAccessFacilities { get; init; } = true;
		public bool TravelActive { get; set; } = true;
		public List<(string Kind, object? Data)> Calls { get; } = [];
		public Action? LastTweenCallback { get; private set; }
		public bool CameraOcclusionEnabled { get; private set; }

		public WorldMapDirector CreateDirector()
		{
			var director = new WorldMapDirector(CreateContext());
			director.RegisterMode(Cinematic);
			director.RegisterMode(Overview);
			director.RegisterMode(Facade);
			return director;
		}

		public void CompleteTween()
		{
			var callback = LastTweenCallback ?? throw new InvalidOperationException("No tween to complete.");
			LastTweenCallback = null;
			callback();
		}

		private MapPresentationContext CreateContext() =>
			new()
			{
				Map = () => StarMap.Create(1),
				ResolvePlayerTravelSample = () => new PlayerTravelSample(
					default,
					TravelActive ? Vector3.Forward : null,
					TravelActive),
				ResolveDockedPoiId = () => DockedPoiId,
				CanAccessFacilities = () => CanAccessFacilities,
				ViewportSize = () => (1920f, 1080f),
				Camera = null!,
				ResolveCameraPose = () => new OrbitPose { Distance = 12f },
				View = null!,
				BoundsHalfX = 16f,
				BoundsHalfZ = 16f,
				ApplyLimits = limits => Calls.Add(("ApplyLimits", limits)),
				SetOcclusionEnabled = enabled => CameraOcclusionEnabled = enabled,
				SnapToPose = (pose, limits) => Calls.Add(("Snap", pose)),
				TweenToPose = (pose, limits, onComplete) =>
				{
					Calls.Add(("Tween", pose));
					LastTweenCallback = onComplete;
				},
			};
	}

	private sealed class StubPresentationMode : IPresentationMode
	{
		public string Id { get; init; } = string.Empty;
		public OrbitLimits Limits { get; init; } = new(4f, 72f, 0.12f, 1.15f);
		public PresentationInputPolicy InputPolicy { get; init; } =
			new(true, true, true, true, true);
		public IReadOnlySet<string> AllowedFrom { get; init; } = new HashSet<string>();
		public string? ExitTargetId { get; init; }
		public bool RequiresFacadePayload { get; init; }
		public bool IsBusy { get; set; }
		public int UpdateCallCount { get; set; }
		public List<string> Lifecycle { get; } = [];

		public PresentationTransitionResult ValidateEnterPayload(object? payload) =>
			Id switch
			{
				"facade" when payload is FacadeEnterPayload => PresentationTransitionResult.Ok(),
				"facade" => PresentationTransitionResult.Fail(PresentationTransitionFailure.InvalidPayload),
				"cinematic" when payload is null => PresentationTransitionResult.Ok(),
				"cinematic" => PresentationTransitionResult.Fail(PresentationTransitionFailure.InvalidPayload),
				_ => PresentationTransitionResult.Ok(),
			};

		public bool CanEnter(MapPresentationContext ctx, string sourceModeId, object? payload)
		{
			if (!RequiresFacadePayload)
				return true;

			if (payload is not FacadeEnterPayload facadePayload)
				return false;

			if (!ctx.CanAccessFacilities())
				return false;

			var dockedPoiId = ctx.ResolveDockedPoiId();
			return dockedPoiId is not null && dockedPoiId == facadePayload.PoiId;
		}

		public OrbitPose ResolveEnterPose(
			string sourceModeId,
			MapPresentationContext ctx,
			object? payload) =>
			new() { Distance = 20f };

		public void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload) =>
			Lifecycle.Add($"Entering:{Id}");

		public void OnSettled(MapPresentationContext ctx) =>
			Lifecycle.Add($"Settled:{Id}");

		public void OnExiting(MapPresentationContext ctx, string targetModeId) =>
			Lifecycle.Add($"Exiting:{Id}->{targetModeId}");

		public void Update(MapPresentationContext ctx, double delta) => UpdateCallCount++;
	}
}
