using GrimSpace.Math.Camera;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation;

namespace GrimSpace.Tests.Presentation;

public sealed class WorldMapDirectorZoomTests
{
	private static readonly OrbitLimits FacadeLimits = new(2f, 7f, 0.26f, 0.79f);
	private static readonly OrbitLimits CinematicLimits = new(9f, 18f, 0.35f, 0.70f);
	private static readonly OrbitLimits OverviewLimits = new(22f, 240f, 0.87f, 1.13f);

	[Fact]
	public void WheelZoomWithinBand_AppliesDistanceDelta()
	{
		var harness = new ZoomHarness { CameraDistance = 12f };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		director.OnWheelZoom(1);

		Assert.NotEmpty(harness.DistanceDeltas);
		Assert.False(director.IsTransitioning);
		Assert.Equal("cinematic", director.CurrentModeId);
	}

	[Fact]
	public void WheelZoomOutFromFacade_EntersCinematic()
	{
		var harness = new ZoomHarness { CameraDistance = FacadeLimits.MaxDistance - 0.05f };
		var director = harness.CreateDirector();
		director.SetInitialMode("facade", new FacadeEnterPayload("poi-a"));

		director.OnWheelZoom(-1);

		Assert.True(director.IsTransitioning);
		Assert.Contains("Exiting:facade->cinematic", harness.Facade.Lifecycle);
	}

	[Fact]
	public void WheelZoomInFromCinematicWithDock_EntersFacade()
	{
		var harness = new ZoomHarness
		{
			CameraDistance = CinematicLimits.MinDistance + 0.05f,
			DockedPoiId = "poi-a",
		};
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		director.OnWheelZoom(1);

		Assert.True(director.IsTransitioning);
		Assert.Contains("Exiting:cinematic->facade", harness.Cinematic.Lifecycle);
	}

	[Fact]
	public void WheelZoomInFromCinematicUndocked_ClampsWithoutTransition()
	{
		var harness = new ZoomHarness
		{
			CameraDistance = CinematicLimits.MinDistance + 0.05f,
			DockedPoiId = null,
		};
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		director.OnWheelZoom(1);

		Assert.False(director.IsTransitioning);
		Assert.Equal("cinematic", director.CurrentModeId);
		Assert.NotEmpty(harness.DistanceDeltas);
		Assert.Equal(CinematicLimits.MinDistance, harness.CameraDistance);
	}

	[Fact]
	public void WheelZoomOutFromCinematic_EntersOverview()
	{
		var harness = new ZoomHarness { CameraDistance = CinematicLimits.MaxDistance - 0.05f };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		director.OnWheelZoom(-1);

		Assert.True(director.IsTransitioning);
		Assert.Contains("Exiting:cinematic->overview", harness.Cinematic.Lifecycle);
	}

	[Fact]
	public void WheelZoomInFromOverview_EntersCinematic()
	{
		var harness = new ZoomHarness { CameraDistance = OverviewLimits.MinDistance + 0.05f };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");
		harness.CompleteTween();
		harness.CameraDistance = OverviewLimits.MinDistance + 0.05f;
		harness.Calls.Clear();

		director.OnWheelZoom(1);

		Assert.True(director.IsTransitioning);
		Assert.Contains("Exiting:overview->cinematic", harness.Overview.Lifecycle);
	}

	[Fact]
	public void WheelZoomIgnoredWhileTransitioning()
	{
		var harness = new ZoomHarness { CameraDistance = CinematicLimits.MaxDistance - 0.05f };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.TryEnter("overview");

		director.OnWheelZoom(-1);

		Assert.Empty(harness.DistanceDeltas);
	}

	[Fact]
	public void WheelZoomIgnoredWhileFacadeBusy()
	{
		var harness = new ZoomHarness { CameraDistance = FacadeLimits.MaxDistance - 0.05f };
		var director = harness.CreateDirector();
		director.SetInitialMode("facade", new FacadeEnterPayload("poi-a"));
		harness.Facade.IsBusy = true;

		director.OnWheelZoom(-1);

		Assert.False(director.IsTransitioning);
		Assert.Empty(harness.DistanceDeltas);
	}

	[Fact]
	public void WheelZoomIgnoredWhileCameraAnimating()
	{
		var harness = new ZoomHarness
		{
			CameraDistance = 12f,
			CameraAnimating = true,
		};
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");

		director.OnWheelZoom(1);

		Assert.Empty(harness.DistanceDeltas);
	}

	[Fact]
	public void EnteringCinematicFromFacade_LandsInteriorToPreventImmediateBounce()
	{
		var harness = new ZoomHarness
		{
			CameraDistance = FacadeLimits.MaxDistance - 0.05f,
			DockedPoiId = "poi-a",
		};
		var director = harness.CreateDirector();
		director.SetInitialMode("facade", new FacadeEnterPayload("poi-a"));
		director.OnWheelZoom(-1);
		harness.CompleteTween();

		var enterPose = harness.LastTweenPose ?? throw new InvalidOperationException("Missing tween pose.");
		Assert.False(
			MapZoomNavigation.WouldCrossInward(enterPose.Distance, CinematicLimits, 1));
	}

	[Fact]
	public void EnteringOverviewFromCinematic_LandsInteriorToPreventImmediateBounce()
	{
		var harness = new ZoomHarness { CameraDistance = CinematicLimits.MaxDistance - 0.05f };
		var director = harness.CreateDirector();
		director.SetInitialMode("cinematic");
		director.OnWheelZoom(-1);
		harness.CompleteTween();

		var enterPose = harness.LastTweenPose ?? throw new InvalidOperationException("Missing tween pose.");
		Assert.False(
			MapZoomNavigation.WouldCrossInward(
				enterPose.Distance,
				OverviewLimits,
				1,
				MapZoomNavigation.OverviewStepPolicy));
	}

	private sealed class ZoomHarness
	{
		public StubPresentationMode Cinematic { get; } = new()
		{
			Id = "cinematic",
			ExitTargetId = null,
			AllowedFrom = new HashSet<string>(StringComparer.Ordinal) { "overview", "facade" },
			Limits = CinematicLimits,
			ResolveEnterDistance = (_, source) =>
				source == "overview"
					? MapZoomNavigation.InteriorDistance(CinematicLimits, fromMinSide: true)
					: source == "facade"
						? MapZoomNavigation.InteriorDistance(CinematicLimits, fromMinSide: false)
						: 12f,
		};

		public StubPresentationMode Overview { get; } = new()
		{
			Id = "overview",
			ExitTargetId = "cinematic",
			AllowedFrom = new HashSet<string>(StringComparer.Ordinal) { "cinematic" },
			Limits = OverviewLimits,
			ResolveEnterDistance = (_, _) =>
				MapZoomNavigation.InteriorDistance(OverviewLimits, fromMinSide: false),
		};

		public StubPresentationMode Facade { get; } = new()
		{
			Id = "facade",
			ExitTargetId = "cinematic",
			AllowedFrom = new HashSet<string>(StringComparer.Ordinal) { "cinematic" },
			RequiresFacadePayload = true,
			Limits = FacadeLimits,
			InputPolicy = new PresentationInputPolicy(true, false, true, false, false),
			ResolveEnterDistance = (_, _) =>
				MapZoomNavigation.InteriorDistance(FacadeLimits, fromMinSide: false),
		};

		public string? DockedPoiId { get; init; } = "poi-a";
		public bool CanAccessFacilities { get; init; } = true;
		public float CameraDistance { get; set; } = 12f;
		public bool CameraAnimating { get; set; }
		public List<float> DistanceDeltas { get; } = [];
		public List<(string Kind, object? Data)> Calls { get; } = [];
		public OrbitPose? LastTweenPose { get; private set; }
		public Action? LastTweenCallback { get; private set; }

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
			if (LastTweenPose is { } pose)
				CameraDistance = pose.Distance;
			LastTweenCallback = null;
			callback();
		}

		private MapPresentationContext CreateContext() =>
			new()
			{
				Map = () => StarMap.Create(1),
				ResolvePlayerTravelSample = () => new PlayerTravelSample(default, null, false),
				ResolveDockedPoiId = () => DockedPoiId,
				CanAccessFacilities = () => CanAccessFacilities,
				ViewportSize = () => (1920f, 1080f),
				Camera = null!,
				ResolveCameraPose = () => new OrbitPose { Distance = CameraDistance },
				IsCameraAnimating = () => CameraAnimating,
				ApplyCameraDistanceDelta = delta =>
				{
					DistanceDeltas.Add(delta);
					CameraDistance = System.Math.Clamp(
						CameraDistance + delta,
						0f,
						float.MaxValue);
				},
				View = null!,
				BoundsHalfX = 16f,
				BoundsHalfZ = 16f,
				ApplyLimits = limits => Calls.Add(("ApplyLimits", limits)),
				SetOcclusionEnabled = _ => { },
				SnapToPose = (pose, limits) => Calls.Add(("Snap", pose)),
				TweenToPose = (pose, limits, onComplete) =>
				{
					Calls.Add(("Tween", pose));
					LastTweenPose = pose;
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
		public Func<string, string, float>? ResolveEnterDistance { get; init; }
		public List<string> Lifecycle { get; } = [];

		public PresentationTransitionResult ValidateEnterPayload(object? payload) =>
			Id switch
			{
				"facade" when payload is FacadeEnterPayload => PresentationTransitionResult.Ok(),
				"facade" => PresentationTransitionResult.Fail(PresentationTransitionFailure.InvalidPayload),
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
			new()
			{
				Distance = ResolveEnterDistance?.Invoke(sourceModeId, Id) ?? 20f,
			};

		public void OnEntering(MapPresentationContext ctx, string sourceModeId, object? payload) =>
			Lifecycle.Add($"Entering:{Id}");

		public void OnSettled(MapPresentationContext ctx) =>
			Lifecycle.Add($"Settled:{Id}");

		public void OnExiting(MapPresentationContext ctx, string targetModeId) =>
			Lifecycle.Add($"Exiting:{Id}->{targetModeId}");

		public void Update(MapPresentationContext ctx, double delta) { }
	}
}
