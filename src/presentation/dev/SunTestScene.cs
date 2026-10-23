using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;
using GrimSpace.Presentation.Graphics;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;

namespace GrimSpace.Presentation.Dev;

/// <summary>
/// Development scene used to render and compare the sun shader, and to check
/// that the star behaves like a light source rather than a sprite.
///
/// Run it with:
/// <code>
/// godot --path &lt;repo&gt; --write-movie sun_test.avi --quit-after 300 --scene res://scenes/sun_test.tscn
/// </code>
/// </summary>
public sealed partial class SunTestScene : Node3D
{
	[Export] public float StarRadius { get; set; } = 1f;

	/// <summary>Camera distance in star radii.</summary>
	[Export] public float CameraDistance { get; set; } = 12f;

	/// <summary>
	/// Fraction of the frame height the disc should fill.  The field of view
	/// is derived from the distance so the framing stays constant and, more
	/// importantly, so the view stays close to the near-orthographic view the
	/// SDO reference frames were taken with.
	/// </summary>
	[Export] public float FrameFill { get; set; } = 0.85f;

	/// <summary>Turns the star slowly so the granulation is visible in motion.</summary>
	[Export] public bool Spin { get; set; }

	[Export] public float SpinSpeed { get; set; } = 0.06f;

	[Export] public float Seed { get; set; } = 4.37f;

	/// <summary>Set false to render the 304 A style red disk instead of visible light.</summary>
	[Export] public bool VisibleLightPalette { get; set; } = true;

	/// <summary>
	/// The post process bloom.  It is off by default: Godot's glow is computed
	/// on a low precision mip chain and it draws a visible dark annulus around
	/// a very bright small source.  The screen space glare pass does the job
	/// instead, and it has no banding.
	/// </summary>
	[Export] public bool Glow { get; set; } = false;

	/// <summary>Debug helper for the surface shader: 1..8 outputs flat grey steps.</summary>
	[Export] public int SurfaceDebug { get; set; }

	/// <summary>Debug helper for the prominence shader.</summary>
	[Export] public int ProminenceDebug { get; set; }

	/// <summary>A close range shot of the prominence loops.</summary>
	[Export] public bool Closeup { get; set; }

	/// <summary>
	/// A telephoto shot of a single prominence at the limb, so the thread
	/// braid, the knots and the feathered edge can be judged.
	/// </summary>
	[Export] public bool ProminenceCloseup { get; set; }

	/// <summary>
	/// The lit-by axis: an asteroid close to the star, lit by the star's own
	/// point light, so the terminator and the crescent read on its surface.
	/// </summary>
	[Export] public bool AsteroidPresence { get; set; }

	/// <summary>
	/// Renders the master branch's red dwarf implementation (the texture
	/// surface plus the billboard corona) so the two can be compared.
	/// </summary>
	[Export] public bool MasterImplementation { get; set; }

	/// <summary>Scales the prominence and corona billboards.</summary>
	[Export] public float QuadScale { get; set; } = 1f;

	/// <summary>
	/// Renders the real battle backdrop instead of a bare star, so the red dwarf
	/// sun can be checked in the scene that actually uses it.
	/// </summary>
	[Export] public bool BattleBackdrop { get; set; }

	/// <summary>Diagnostic: render the map-star reproduction without the
	/// glare/dip overlay (the S1b evidence isolation).</summary>
	[Export] public bool NoGlare { get; set; }

	/// <summary>
	/// The presence test: the star drifts across a rock at close range, with the
	/// rock lit by the star's own light.
	/// </summary>
	[Export] public bool PresenceTest { get; set; }

	/// <summary>Turns the screen space glare and the point light off (the "before").</summary>
	[Export] public bool Legacy { get; set; }

	/// <summary>
	/// Reproduction mode for the STAR MAP's star: it builds the star exactly
	/// the way MapCelestialVisuals (the map's palette owner) configures it, at
	/// the map star's world radius, under the map camera's geometry.  This is
	/// the harness the map-star defect sheets are rendered from.
	/// </summary>
	[Export] public bool MapStar { get; set; }

	private Node3D? _star;
	private SunGlareOverlay? _glare;
	private Godot.Collections.Array _events = [];
	private Godot.Collections.Array _spots = [];
	private readonly SunPhenomena _phenomena = new();
	private ulong _timelineOrigin;
	private AudioStreamPlayer? _sunAudio;
	private float _timelineDuration = 14f;
	private const string SunRumblePath = "res://dist/sun_rumble_map.wav";

	public override void _Ready()
	{
		ApplyCommandLineOverrides();

		// The phenomena timeline is clip relative, so it lines up with the
		// soundtrack's event list.
		_timelineOrigin = Time.GetTicksMsec();
		RenderingServer.SetDefaultClearColor(new Color(0f, 0f, 0f, 1f));
		AddChild(BuildEnvironment());
		LoadPhenomenaTimeline();

		var palette = BuildPalette();

		_star = SunVisual.Create(StarRadius, palette);
		AddChild(_star);

		if (MapStar)
		{
			BuildMapStarScene();
			return;
		}

		if (BattleBackdrop)
		{
			BuildBattleBackdrop();
			return;
		}

		if (PresenceTest)
		{
			BuildPresenceScene();
			return;
		}

		if (Closeup)
		{
			BuildCloseupScene();
			return;
		}

		if (MasterImplementation)
		{
			BuildMasterSun();
			return;
		}

		if (AsteroidPresence)
		{
			BuildAsteroidScene();
			return;
		}

		if (ProminenceCloseup)
		{
			BuildProminenceCloseup();
			return;
		}

		BuildShowcaseScene();
	}

	private void LoadPhenomenaTimeline()
	{
		const string path = "res://assets/data/sun_events.json";
		if (!Godot.FileAccess.FileExists(path))
			return;
		var parsed = Json.ParseString(Godot.FileAccess.GetFileAsString(path));
		if (parsed.VariantType != Variant.Type.Dictionary)
			return;
		var dictionary = parsed.AsGodotDictionary();
		if (dictionary.ContainsKey("events"))
			_events = dictionary["events"].AsGodotArray();
		if (dictionary.ContainsKey("spots"))
			_spots = dictionary["spots"].AsGodotArray();
		_timelineDuration = SunPhenomena.ReadTimelineDuration(path);
		// The casting office (S2': one role at a time) is driven by the same
		// timeline's roll schedule — one source of truth for picture and sound.
		SunVisual.SetProminenceSchedule(SunPhenomena.LoadRoles(path));
		GD.Print($"[SunTest] phenomena timeline: {_events.Count} events, {_spots.Count} spots");
	}

	private SunPalette BuildPalette()
	{
		if (MapStar)
		{
			// The reproduction of the map's own star: the palette comes from
			// the map's palette owner, exactly as the strategic map plays it.
			StarRadius = 4f; // Star.DefaultRadius 128 grid points * 1/32 world
			return MapCelestialVisuals.BuildStarPalette(Mathf.RoundToInt(Seed * 7.0f), MapAtmosphereSettings.Default);
		}

		if (VisibleLightPalette)
		{
			return new SunPalette
			{
				Seed = Seed,
				SurfaceDebug = SurfaceDebug,
				ProminenceDebug = ProminenceDebug,
				ProminenceQuadWidth = 10f * QuadScale,
			};
		}

		return new SunPalette
		{
			// Colours sampled from the AIA 304 reference frame:
			// centre rgb(0.718, 0.061, 0.003), 0.90R rgb(0.563, 0.040, 0.005).
			CoreColor = new Color(0.75f, 0.17f, 0.03f),
			MidColor = new Color(0.70f, 0.06f, 0.015f),
			LimbColor = new Color(0.30f, 0.006f, 0.002f),
			ProminenceColor = new Color(0.55f, 0.10f, 0.09f),
			FootpointColor = new Color(0.75f, 0.22f, 0.10f),
			LimbRingColor = new Color(0.80f, 0.16f, 0.05f),
			CoronaColor = new Color(1.00f, 0.30f, 0.10f),
			EmissionStrength = 0.17f,
			GranulationCells = 176f,
			GranulationStrength = 0.85f,
			PlageCells = 13f,
			PlageStrength = 0.60f,
			SupergranulationCells = 40f,
			SupergranulationStrength = 0.18f,
			RimColor = new Color(1.00f, 0.16f, 0.05f),
			RimStrength = 0.42f,
			LimbEdgeStrength = 1.0f,
			LimbEdgeFloor = 0.30f,
			LimbDarkening = 0.15f,
			LimbDarkeningSquared = 0.05f,
			ProminenceStrength = 0.28f,
			CoronaStrength = 0.006f,
			InnerCoronaStrength = 0.05f,
			LimbRingStrength = 0.03f,
			SpiculeHaze = 0.05f,
			CoreBurn = 0.0f,
			TextureBlend = 0.0f,
			LightEnergy = 0f,
			Seed = Seed,
			SurfaceDebug = SurfaceDebug,
			ProminenceDebug = ProminenceDebug,
			ProminenceQuadWidth = 10f * QuadScale,
		};
	}

	private WorldEnvironment BuildEnvironment()
	{
		var stars = GD.Load<Texture2D>("res://assets/textures/8k_stars.jpg");
		return new WorldEnvironment
		{
			Name = "Environment",
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Sky,
				Sky = new Sky
				{
					SkyMaterial = new PanoramaSkyMaterial
					{
						Panorama = stars,
						EnergyMultiplier = 0.35f,
					},
				},
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.05f, 0.05f, 0.07f),
				AmbientLightEnergy = 0.1f,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
				TonemapMode = Godot.Environment.ToneMapper.Linear,
				TonemapExposure = 1.0f,
				GlowEnabled = Glow,
				GlowIntensity = 0.30f,
				GlowBloom = 0.0f,
				GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive,
				GlowStrength = 1.0f,
				GlowHdrThreshold = 1.0f,
			},
		};
	}

	private void BuildShowcaseScene()
	{
		var angularRadius = Mathf.Asin(Mathf.Min(1f / Mathf.Max(CameraDistance, 1.001f), 0.999f));
		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(0f, 0f, CameraDistance * StarRadius),
			Fov = Mathf.RadToDeg(angularRadius) * 2f / Mathf.Max(FrameFill, 0.05f),
			Current = true,
		};
		AddChild(camera);
		AddGlare();
		GD.Print($"[SunTest] showcase: distance={CameraDistance} fov={camera.Fov:0.00} spin={Spin}");
	}

	/// <summary>
	/// The master branch's own sun: the 2k_sun.jpg texture through
	/// battle_red_dwarf.gdshader, plus the billboard corona.  Used to check
	/// whether it has the same dark band between the surface and the glow.
	/// </summary>
	private void BuildMasterSun()
	{
		var surfaceMaterial = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://assets/shaders/battle_red_dwarf.gdshader"),
		};
		surfaceMaterial.SetShaderParameter(
			"surface_texture",
			GD.Load<Texture2D>("res://assets/textures/2k_sun.jpg"));
		var root = new Node3D { Name = "MasterSun" };
		root.AddChild(new MeshInstance3D
		{
			Name = "Surface",
			Mesh = new SphereMesh
			{
				Radius = StarRadius,
				Height = StarRadius * 2f,
				RadialSegments = 64,
				Rings = 32,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = surfaceMaterial,
		});
		root.AddChild(StarCorona.Create(
			StarRadius,
			new Color(1f, 0.12f, 0.025f),
			brightness: 0.2f,
			width: 0.18f,
			speed: 0.025f,
			seed: 37f,
			irregularity: 0.82f));
		AddChild(root);
		_star = root;

		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(-0.55f, 0f, 2.45f) * StarRadius,
			Fov = 50f,
			Current = true,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(0.85f, 0.12f, 0.62f) * StarRadius, Vector3.Up);
		GD.Print("[SunTest] master implementation");
	}

	/// <summary>
	/// The shipped asteroid pack is a metallic variant, and a metal only shows
	/// a specular highlight, so the rock would read as a black silhouette
	/// instead of a lit body.  The material is replaced with a rough
	/// dielectric so the star's light falls on it and the terminator reads.
	/// </summary>
	/// <summary>
	/// The shader's site hash, replicated so the camera can aim at a real
	/// prominence.  A prominence is a screen space feature: the billboard
	/// faces the camera, so site i always sits at the same screen angle.
	/// </summary>
	private static float SiteHash(float n) =>
		(float)((System.Math.Sin(n * 127.1f + 311.7f) * 43758.5453123f) % 1.0);

	/// <summary>
	/// A telephoto shot of one prominence.  The camera stays on the star's
	/// axis (so the billboard's basis is the world X/Y plane) and is aimed at
	/// the site, with a narrow field of view so the loop fills the frame.
	/// </summary>
	private void BuildProminenceCloseup()
	{
		_star!.Position = Vector3.Zero;

		// Pick the site with the largest height scale among the casting
		// office's holder pool (S2': four holders, one role at a time).
		var bestSite = 0;
		var bestScale = -1f;
		for (var i = 0; i < SunProminenceCasting.HolderCount; i++)
		{
			var h3 = SiteHash(i * 47.33f + Seed + 11.9f);
			var scale = 0.45f + 0.85f * h3;
			if (scale > bestScale)
			{
				bestScale = scale;
				bestSite = i;
			}
		}
		var angle = SiteHash(bestSite * 17.13f + Seed) * Mathf.Tau;

		// Aim at the crown of the loop, a little outside the limb, so the
		// visible half of the prominence fills the frame.
		// The billboard's V axis runs down the screen, so the site's screen
		// angle maps to the world with the vertical component flipped.
		var target = new Vector3(
			Mathf.Cos(angle) * 1.02f,
			Mathf.Sin(angle) * 1.02f,
			0f) * StarRadius;

		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(0f, 0f, 3.0f) * StarRadius,
			// A long lens: the visible half of a 0.26 star radius loop has to
			// fill roughly 40% of the frame height.
			Fov = 9.5f,
			Current = true,
		};
		AddChild(camera);
		camera.LookAt(target, Vector3.Up);
		AddGlare();
		GD.Print($"[SunTest] prominence closeup: site {bestSite} at {angle:0.00} rad, scale {bestScale:0.00}");
	}

	private static void MakeRockLit(Node root)
	{
		var material = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.52f, 0.46f, 0.40f),
			Roughness = 0.88f,
			Metallic = 0.0f,
		};
		foreach (var child in root.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				mesh.MaterialOverride = material;
			}
			MakeRockLit(child);
		}
	}

	/// <summary>
	/// Axis B: the star, an asteroid beside it, and the light the star throws
	/// on the asteroid's surface.
	/// </summary>
	private void BuildAsteroidScene()
	{
		_star!.Position = Vector3.Zero;

		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(0f, 0.15f, 5.4f) * StarRadius,
			Fov = 52f,
			Current = true,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(0.62f, -0.16f, 0f) * StarRadius, Vector3.Up);

		const string packPath =
			"res://assets/models/asteroids/asteroids_pack_metallic_version.glb";
		var pack = GD.Load<PackedScene>(packPath);
		if (pack is not null)
		{
			var asteroid = pack.Instantiate<Node3D>();
			asteroid.Name = "Asteroid";
			asteroid.Scale = Vector3.One * 0.085f;
			asteroid.Position = new Vector3(1.30f, -0.30f, 0.35f) * StarRadius;
			AddChild(asteroid);
			MakeRockLit(asteroid);
		}
		else
		{
			var rock = GD.Load<PackedScene>("res://assets/models/asteroids/rock.glb");
			if (rock is not null)
			{
				var asteroid = rock.Instantiate<Node3D>();
				asteroid.Name = "Asteroid";
				asteroid.Scale = Vector3.One * 0.45f;
				asteroid.Position = new Vector3(1.62f, -0.42f, 0.15f) * StarRadius;
				AddChild(asteroid);
				MakeRockLit(asteroid);
			}
		}

		AddGlare();
		GD.Print("[SunTest] asteroid presence scene");
	}

	/// <summary>
	/// The presence test.  A rock sits between the camera and the star while
	/// the star drifts across the frame.  The rock is lit by the star's own
	/// point light, so the falloff across it is visible.
	/// </summary>
	private void BuildPresenceScene()
	{
		// The star sits off to the side, so its light falls across the objects
		// instead of silhouetting them.
		_star!.Position = new Vector3(-3.1f, 0.35f, -4.6f);

		// A rock close to the star, so the falloff of the star's light across
		// it is visible, and a hull further out that catches only the rim.
		var rock = new MeshInstance3D
		{
			Name = "Rock",
			Mesh = new SphereMesh { Radius = 0.30f, Height = 0.60f, RadialSegments = 24, Rings = 12 },
			Position = new Vector3(-0.15f, -0.42f, -3.30f),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.46f, 0.43f, 0.40f),
				Roughness = 0.95f,
			},
		};
		AddChild(rock);

		var hull = new MeshInstance3D
		{
			Name = "Hull",
			Mesh = new BoxMesh { Size = new Vector3(0.34f, 0.22f, 0.85f) },
			Position = new Vector3(0.70f, 0.30f, -3.70f),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.40f, 0.42f, 0.46f),
				Roughness = 0.55f,
				Metallic = 0.6f,
			},
		};
		AddChild(hull);

		var camera = new Camera3D
		{
			Name = "Camera",
			Position = Vector3.Zero,
			Fov = 55f,
			Current = true,
		};
		AddChild(camera);
		AddGlare();
		GD.Print("[SunTest] presence test: star drifting across a lit rock");
	}

	/// <summary>
	/// A close range shot: the camera sits just outside the photosphere so the
	/// prominence loops fill the frame and their thread structure can be judged.
	/// </summary>
	private void BuildCloseupScene()
	{
		// Sit just outside the photosphere and look along the limb, so the
		// prominence loops fill the frame.
		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(-0.55f, 0f, 2.45f) * StarRadius,
			Fov = 50f,
			Current = true,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(0.85f, 0.12f, 0.62f) * StarRadius, Vector3.Up);
		AddGlare();
		GD.Print("[SunTest] closeup: camera along the limb");
	}

	private void BuildBattleBackdrop()
	{
		var grid = new Grid(9, 9, 9);
		var backdrop = new SpaceBackdrop();
		backdrop.Build(grid, 20240508);
		AddChild(backdrop);

		var center = WorldMapping.GridCenter(grid);
		var halfExtent = new Vector3(
			grid.Width * WorldMapping.CellSize,
			grid.Height * WorldMapping.CellSize,
			grid.Depth * WorldMapping.CellSize) * 0.5f;
		var chamberRadius = halfExtent.Length();

		var light = new DirectionalLight3D { Name = "SunLight" };
		AddChild(light);
		RedDwarfSun.Configure(light, center, chamberRadius);

		var camera = new Camera3D
		{
			Name = "Camera",
			Position = center + RedDwarfSun.LightDirection * chamberRadius * 0.9f,
			Current = true,
		};
		AddChild(camera);
		camera.LookAt(
			center - RedDwarfSun.LightDirection * chamberRadius * 2.4f,
			Vector3.Up);
		GD.Print($"[SunTest] battle backdrop built, chamber radius {chamberRadius:0.0}");
	}

	private void BuildMapStarScene()
	{
		// The star map's opening pose (MapCinematicFraming): behind the focal
		// point looking out so the frame faces the star — the S3 presence
		// framing — at the map cinematic distance of 6 world units over a
		// pivot where the player sits, with a second wide quick check view.
		var starDistance = CameraDistance * StarRadius;
		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(0f, starDistance * 0.34f, starDistance * 0.98f),
			Fov = 34f, // the map camera's own field of view
			Far = 400f,
			Current = true,
		};
		AddChild(camera);
		camera.LookAt(Vector3.Zero, Vector3.Up);
		if (!NoGlare)
		{
			AddGlare();
			_glare!.OpeningSeconds = 1.4f; // the "opening on the sun" beat
		}

		// S4 evidence path: the map plays the pipeline rumble on the Sun bus,
		// and the timeline clock is the audio's playback position — so the
		// rendered AVI carries the mix the map actually plays, synced.
		if (ResourceLoader.Exists(SunRumblePath))
		{
			_sunAudio = new AudioStreamPlayer
			{
				Bus = GrimSpace.Application.AudioBuses.SunAmbience,
				VolumeDb = -8f,
				Stream = GD.Load<AudioStream>(SunRumblePath),
			};
			_sunAudio.Finished += () => _sunAudio!.Play();
			AddChild(_sunAudio);
			_sunAudio.Play();
		}
		else
		{
			GD.PushWarning("[SunTest] map-star: rumble not built (scripts/make-sun-audio.py); silent render.");
		}
		GD.Print($"[SunTest] map-star reproduction: radius {StarRadius:0.00} world, camera {starDistance:0.00} out");
	}

	private void AddGlare()
	{
		// The reference frames are spacecraft images: no lens flare, no
		// exposure dip.  The diagnostic palette therefore renders without them.
		if (Legacy || _star is null || !VisibleLightPalette)
			return;
		_glare = SunVisual.CreateGlareOverlay(_star, StarRadius);
		_glare.Intensity = 0.85f;
		_glare.VignetteStrength = 0.16f;
		_glare.StreakAmount = 0.20f;
		_glare.Chromatic = 0.35f;
		_glare.Breathe = 0.10f;
		AddChild(_glare);
	}

	public override void _Process(double delta)
	{
		if (_events.Count > 0 || _spots.Count > 0)
		{
			// The timeline clock is the audio's playback position when the
			// rumble plays (picture and sound share the period); otherwise
			// the frame clock keeps the timeline alive.
			float time = _sunAudio is not null
				? ((float)_sunAudio.GetPlaybackPosition() % Mathf.Max(_timelineDuration, 0.01f))
				: 0f;
			if (_sunAudio is null)
				time = (Time.GetTicksMsec() - _timelineOrigin) / 1000f;
			SunPhenomena.Evaluate(_events, _spots, time, _phenomena);
			SunVisual.UpdatePhenomena(_phenomena, time);
		}

		if (Spin && _star is not null)
			_star.RotateY((float)delta * SpinSpeed);

		if (PresenceTest && _star is not null)
		{
			// The star drifts across the frame, passing behind the rock.
			var t = (float)Time.GetTicksMsec() / 1000f;
			_star.Position = new Vector3(
				-3.1f + Mathf.Sin(t * 0.42f) * 1.5f,
				0.35f + Mathf.Cos(t * 0.27f) * 0.30f,
				-4.6f);
		}
	}

	/// <summary>
	/// Reads overrides from the user arguments (everything after a bare
	/// <c>--</c> on the command line), so one scene can be used both for the
	/// SDO-matched reference comparison and for the showcase clip.
	/// </summary>
	private void ApplyCommandLineOverrides()
	{
		foreach (var argument in OS.GetCmdlineUserArgs())
		{
			if (argument == "--spin")
			{
				Spin = true;
			}
			else if (argument == "--noglow")
			{
				Glow = false;
			}
			else if (argument == "--304")
			{
				VisibleLightPalette = false;
			}
			else if (argument == "--battle")
			{
				BattleBackdrop = true;
			}
			else if (argument == "--presence")
			{
				PresenceTest = true;
			}
			else if (argument == "--mapstar")
			{
				MapStar = true;
			}
			else if (argument is "--noglare" or "--no-glare")
			{
				// Both spellings: audits type either; an inert flag is a lie.
				NoGlare = true;
			}
			else if (argument == "--noglare")
			{
				NoGlare = true;
			}
			else if (argument == "--closeup")
			{
				Closeup = true;
			}
			else if (argument == "--master")
			{
				MasterImplementation = true;
			}
			else if (argument == "--asteroid")
			{
				AsteroidPresence = true;
			}
			else if (argument == "--prominence")
			{
				ProminenceCloseup = true;
			}
			else if (argument == "--legacy")
			{
				Legacy = true;
			}
			else if (argument.StartsWith("--spin=", StringComparison.Ordinal))
			{
				Spin = true;
				SpinSpeed = ParseFloat(argument[7..], SpinSpeed);
			}
			else if (argument.StartsWith("--camdist=", StringComparison.Ordinal))
			{
				CameraDistance = ParseFloat(argument[10..], CameraDistance);
			}
			else if (argument.StartsWith("--seed=", StringComparison.Ordinal))
			{
				Seed = ParseFloat(argument[7..], Seed);
			}
			else if (argument.StartsWith("--debug=", StringComparison.Ordinal))
			{
				ProminenceDebug = Mathf.RoundToInt(ParseFloat(argument[8..], 0f));
			}
			else if (argument.StartsWith("--quadscale=", StringComparison.Ordinal))
			{
				QuadScale = ParseFloat(argument[12..], 1f);
			}
			else if (argument.StartsWith("--surfacedebug=", StringComparison.Ordinal))
			{
				SurfaceDebug = Mathf.RoundToInt(ParseFloat(argument[15..], 0f));
			}
		}
	}

	private static float ParseFloat(string text, float fallback) =>
		float.TryParse(text, System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out var value)
			? value
			: fallback;
}
