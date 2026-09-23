using Godot;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Presentation.Atmosphere;
using GrimSpace.World.StarSystem.Presentation.Map;
using GrimSpace.World.StarSystem.Presentation.Picking;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public partial class NavigationLandmarkShowcaseController : Node3D
{
	[Export]
	public ENavigationLandmarkKind Kind { get; set; } = ENavigationLandmarkKind.DustCloud;

	[Export]
	public bool SingleSmokeBillboard { get; set; } = true;

	[Export]
	public int GridRadius { get; set; } = 40;

	[Export]
	public int VisualSeed { get; set; } = 1553554798;

	[Export]
	public bool ShowNavigationFootprint { get; set; } = true;

	[Export]
	public bool ShowLocalBounds { get; set; } = true;

	private Node3D _landmarkRoot = null!;
	private Label _status = null!;

	public override void _Ready()
	{
		_status = GetNode<Label>("UI/Status");
		Rebuild();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Tab:
				Kind = (ENavigationLandmarkKind)(((int)Kind + 1) % Enum.GetValues<ENavigationLandmarkKind>().Length);
				Rebuild();
				break;
			case Key.R:
				VisualSeed = (int)(GD.Randi() % int.MaxValue);
				Rebuild();
				break;
			case Key.F:
				ShowNavigationFootprint = !ShowNavigationFootprint;
				Rebuild();
				break;
			case Key.B:
				ShowLocalBounds = !ShowLocalBounds;
				Rebuild();
				break;
			case Key.M:
				SingleSmokeBillboard = !SingleSmokeBillboard;
				Rebuild();
				break;
		}
	}

	private void Rebuild()
	{
		foreach (var child in GetChildren().ToArray())
		{
			if (child.Name == "ScalePoi" || child.Name == "ScaleShip")
			{
				RemoveChild(child);
				child.Free();
			}
		}

		if (_landmarkRoot is not null)
		{
			_landmarkRoot.QueueFree();
			_landmarkRoot = null!;
		}

		var landmark = new NavigationLandmark(
			"showcase",
			"Showcase",
			Kind,
			new GrimSpace.Math.Grid.Coord(0, 0, 0),
			GridRadius,
			VisualSeed);

		_landmarkRoot = new Node3D { Name = "ShowcaseLandmark" };
		if (SingleSmokeBillboard)
		{
			var worldRadius = MapMapping.ToWorldRadius(GridRadius);
			var texture = MapSmokeDustVisuals.LoadDefaultSmokeTexture();
			_landmarkRoot.AddChild(MapSmokeDustVisuals.CreateSingleBillboardLayer(
				texture,
				worldRadius * 0.9f,
				0.12f,
				NavigationLandmarkPalette.DustCardColor(VisualSeed, 0, CreateShowcaseRng(), 0.12f)));
		}
		else
		{
			_landmarkRoot = NavigationLandmarkVisualCatalog.Build(landmark);
		}

		AddChild(_landmarkRoot);

		if (ShowNavigationFootprint)
			_landmarkRoot.AddChild(BuildFootprintRing(GridRadius));

		if (ShowLocalBounds)
			_landmarkRoot.AddChild(BuildBoundsRing());

		AddScaleReferences();
		var mode = SingleSmokeBillboard ? "single smoke billboard" : Kind.ToString();
		_status.Text =
			$"{mode} · radius {GridRadius} · visual seed {VisualSeed}\n"
			+ "M single billboard · Tab cycle kind · R reseed · F footprint · B local bounds";
	}

	private void AddScaleReferences()
	{
		var poiRoot = new Node3D { Name = "ScalePoi" };
		poiRoot.Position = new Vector3(MapMapping.ToWorldRadius(GridRadius) * 1.6f, 0f, 0f);
		MapCelestialVisuals.AddMoonlet(poiRoot, VisualSeed + 17, MapMapping.ToWorldRadius(18));
		AddChild(poiRoot);

		var ship = new MeshInstance3D
		{
			Name = "ScaleShip",
			Position = new Vector3(-MapMapping.ToWorldRadius(GridRadius) * 1.2f, 0.06f, 0f),
			Mesh = new PrismMesh { Size = new Vector3(0.18f, 0.04f, 0.28f) },
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(0.42f, 0.78f, 0.86f),
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(ship);
	}

	private static MeshInstance3D BuildFootprintRing(int gridRadius)
	{
		const int segments = 48;
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(new Color(0.41f, 0.69f, 0.76f, 0.4f));
		var radius = MapMapping.ToWorldRadius(gridRadius);
		for (var i = 0; i < segments; i++)
		{
			var a = i * Mathf.Tau / segments;
			var b = (i + 1) * Mathf.Tau / segments;
			mesh.SurfaceAddVertex(new Vector3(Mathf.Cos(a) * radius, 0.02f, Mathf.Sin(a) * radius));
			mesh.SurfaceAddVertex(new Vector3(Mathf.Cos(b) * radius, 0.02f, Mathf.Sin(b) * radius));
		}

		mesh.SurfaceEnd();
		return new MeshInstance3D
		{
			Name = "DebugFootprint",
			Mesh = mesh,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
	}

	private RandomNumberGenerator CreateShowcaseRng()
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)VisualSeed };
		return rng;
	}

	private MeshInstance3D BuildBoundsRing()
	{
		var bounds = NavigationLandmarkVisualCatalog.MeasureLocalBounds(_landmarkRoot);
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(new Color(0.82f, 0.62f, 0.38f, 0.45f));
		var min = bounds.Position;
		var max = bounds.Position + bounds.Size;
		var y = 0.03f;
		Vector3[] corners =
		[
			new(min.X, y, min.Z),
			new(max.X, y, min.Z),
			new(max.X, y, max.Z),
			new(min.X, y, max.Z),
		];
		for (var i = 0; i < corners.Length; i++)
		{
			mesh.SurfaceAddVertex(corners[i]);
			mesh.SurfaceAddVertex(corners[(i + 1) % corners.Length]);
		}

		mesh.SurfaceEnd();
		return new MeshInstance3D
		{
			Name = "DebugBounds",
			Mesh = mesh,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
	}
}
