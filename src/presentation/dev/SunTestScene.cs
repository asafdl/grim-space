using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;
using GrimSpace.Presentation.Graphics;

namespace GrimSpace.Presentation.Dev;

/// <summary>
/// Development scene that renders the sun shader on its own against a black
/// background, so the result can be recorded and compared frame by frame with
/// the NASA/SDO reference frames in <c>refs/</c>.
///
/// Run it with:
/// <code>
/// godot --path &lt;repo&gt; --write-movie sun_test.avi --quit-after 300 -s res://scenes/sun_test.tscn
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

	/// <summary>Bloom is on by default; the NASA reference frames have none.</summary>
	[Export] public bool Glow { get; set; } = true;

	/// <summary>Debug helper passed through to the prominence shader.</summary>
	[Export] public int ProminenceDebug { get; set; }

	/// <summary>Scales the prominence and corona billboards.</summary>
	[Export] public float QuadScale { get; set; } = 1f;

	/// <summary>Debug helper for the surface shader: 1..8 outputs flat grey steps.</summary>
	[Export] public int SurfaceDebug { get; set; }

	/// <summary>
	/// Renders the real battle backdrop instead of a bare star, so the red dwarf
	/// sun can be checked in the scene that actually uses it.
	/// </summary>
	[Export] public bool BattleBackdrop { get; set; }

	private Node3D? _star;

	public override void _Ready()
	{
		ApplyCommandLineOverrides();

		RenderingServer.SetDefaultClearColor(new Color(0f, 0f, 0f, 1f));

		var environment = new WorldEnvironment
		{
			Name = "Environment",
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0f, 0f, 0f, 1f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.05f, 0.05f, 0.07f),
				AmbientLightEnergy = 0.1f,
				ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
				TonemapMode = Godot.Environment.ToneMapper.Linear,
				TonemapExposure = 1.0f,
				GlowEnabled = Glow,
				GlowIntensity = 0.45f,
				GlowBloom = 0.10f,
				GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive,
				GlowStrength = 1.0f,
			},
		};
		AddChild(environment);

		var palette = VisibleLightPalette
			? new SunPalette
			{
				Seed = Seed,
				ProminenceDebug = ProminenceDebug, SurfaceDebug = SurfaceDebug,
				ProminenceQuadWidth = 4f * QuadScale,
				CoronaQuadWidth = 6f * QuadScale,
			}
			: new SunPalette
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
				EmissionStrength = 0.33f,
				GranulationCells = 176f,
				GranulationStrength = 0.85f,
				PlageCells = 13f,
				PlageStrength = 0.60f,
				RimColor = new Color(1.00f, 0.16f, 0.05f),
				RimStrength = 0.10f,
				LimbEdgeStrength = 1.0f,
				LimbDarkening = 0.15f,
				LimbDarkeningSquared = 0.05f,
				ProminenceStrength = 0.28f,
				CoronaStrength = 0.05f,
				Seed = Seed,
				ProminenceDebug = ProminenceDebug, SurfaceDebug = SurfaceDebug,
				ProminenceQuadWidth = 4f * QuadScale,
				CoronaQuadWidth = 6f * QuadScale,
			};

		_star = SunVisual.Create(StarRadius, palette);
		AddChild(_star);

		if (BattleBackdrop)
		{
			BuildBattleBackdrop();
			return;
		}

		var angularRadius = Mathf.Asin(Mathf.Min(1f / Mathf.Max(CameraDistance, 1.001f), 0.999f));
		var camera = new Camera3D
		{
			Name = "Camera",
			Position = new Vector3(0f, 0f, CameraDistance * StarRadius),
			Fov = Mathf.RadToDeg(angularRadius) * 2f / Mathf.Max(FrameFill, 0.05f),
			Current = true,
		};
		AddChild(camera);

		GD.Print($"[SunTest] radius={StarRadius} camera_distance={CameraDistance} fov={camera.Fov:0.00} spin={Spin}");
	}

	/// <summary>
	/// Builds the same backdrop the battle scene builds, so the red dwarf sun
	/// is verified in the scene that actually uses it.
	/// </summary>
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

	public override void _Process(double delta)
	{
		if (!Spin || _star is null)
			return;
		_star.RotateY((float)delta * SpinSpeed);
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
			else if (argument == "--304")
			{
				VisibleLightPalette = false;
			}
			else if (argument.StartsWith("--surfacedebug=", StringComparison.Ordinal))
			{
				SurfaceDebug = Mathf.RoundToInt(ParseFloat(argument[15..], 0f));
			}
			else if (argument == "--battle")
			{
				BattleBackdrop = true;
			}
		}
	}

	private static float ParseFloat(string text, float fallback) =>
		float.TryParse(text, System.Globalization.NumberStyles.Float,
			System.Globalization.CultureInfo.InvariantCulture, out var value)
			? value
			: fallback;
}
