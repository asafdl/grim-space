using Godot;

namespace GrimSpace.Ui.Portraits;

public sealed record RobotPortraitPalette
{
	public static RobotPortraitPalette FromColors(RobotPortraitColors colors) => new()
	{
		Casing = new Color(colors.Casing),
		Screen = new Color(colors.Screen),
		Glyph = new Color(colors.Glyph),
		Ears = new Color(colors.Ears),
		Antenna = new Color(colors.Antenna),
		Shoulders = new Color(colors.Shoulders),
		Chest = new Color(colors.Chest),
	};

	public Color Casing { get; init; } = new("#778067");
	public Color Screen { get; init; } = new("#20362b");
	public Color Glyph { get; init; } = new("#ccdfa0");
	public Color Ears { get; init; } = new("#606d59");
	public Color Antenna { get; init; } = new("#9ba582");
	public Color Shoulders { get; init; } = new("#858b6b");
	public Color Chest { get; init; } = new("#505b53");
}

public enum RobotPortraitLayer { All, Eyes, Mouth, Screen, Ears, Antenna, Shoulders, Chest, Cracks, Tape, Rust }

public sealed partial class RobotPortrait : Control
{
	private const float CanvasSize = 320f;
	private const string AssetRoot = "res://assets/ui/robot_portraits";
	private readonly TextureRect _backdrop;
	private readonly TextureRect _chest;
	private readonly TextureRect _shoulders;
	private readonly TextureRect _ears;
	private readonly TextureRect _antenna;
	private readonly TextureRect _bezel;
	private readonly TextureRect _screen;
	private readonly TextureRect _eyes;
	private readonly TextureRect _mouth;
	private readonly TextureRect _rust;
	private readonly TextureRect _cracks;
	private readonly TextureRect _tape;
	private RobotPortraitParts _parts = new();
	private RobotPortraitPalette _palette = new();
	private RobotPortraitLayer _layer;
	private float _eyeOpenness = 1f;
	private double _blinkWait = 6 + Random.Shared.NextDouble() * 39;
	private double _blinkTime = -1;

	public event Action? EyeOpennessChanged;

	public RobotPortraitParts Parts
	{
		get => _parts;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			Validate(value);
			_parts = value;
			UpdateTextures();
			LayoutLayers();
		}
	}

	public RobotPortraitPalette Palette
	{
		get => _palette;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			_palette = value;
			UpdateColors();
		}
	}

	public RobotPortraitLayer Layer
	{
		get => _layer;
		set
		{
			if (!Enum.IsDefined(value))
				throw new ArgumentOutOfRangeException(nameof(value));
			_layer = value;
			UpdateVisibility();
		}
	}

	public float EyeOpenness
	{
		get => _eyeOpenness;
		set
		{
			if (!float.IsFinite(value) || value < 0f || value > 1f)
				throw new ArgumentOutOfRangeException(nameof(value), "Eye openness must be between zero and one.");
			if (_eyeOpenness == value)
				return;
			_eyeOpenness = value;
			LayoutLayers();
			EyeOpennessChanged?.Invoke();
		}
	}

	public RobotPortrait()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_backdrop = AddLayer();
		_chest = AddLayer();
		_shoulders = AddLayer();
		_ears = AddLayer();
		_antenna = AddLayer();
		_bezel = AddLayer();
		_screen = AddLayer();
		_eyes = AddLayer();
		_mouth = AddLayer();
		_rust = AddLayer();
		_cracks = AddLayer();
		_tape = AddLayer();
		var glow = GD.Load<Shader>($"{AssetRoot}/face_glow.gdshader")
			?? throw new InvalidOperationException("Could not load robot face glow shader.");
		var glowMaterial = new ShaderMaterial { Shader = glow };
		_eyes.Material = glowMaterial;
		_mouth.Material = glowMaterial;
		Resized += LayoutLayers;
	}

	public override void _Ready()
	{
		UpdateTextures();
		UpdateColors();
		UpdateVisibility();
		LayoutLayers();
	}

	public override void _Process(double delta)
	{
		if (!IsVisibleInTree() || Layer != RobotPortraitLayer.All)
			return;

		if (_blinkTime < 0)
		{
			_blinkWait -= delta;
			if (_blinkWait > 0)
				return;
			_blinkTime = 0;
		}

		_blinkTime += delta;
		if (_blinkTime >= .16)
		{
			EyeOpenness = 1f;
			_blinkTime = -1;
			_blinkWait = 6 + Random.Shared.NextDouble() * 39;
			return;
		}
		EyeOpenness = (float)(_blinkTime < .06
			? 1 - _blinkTime / .06
			: (_blinkTime - .06) / .10);
	}

	private TextureRect AddLayer()
	{
		var image = new TextureRect
		{
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
		};
		AddChild(image);
		return image;
	}

	private void UpdateTextures()
	{
		_backdrop.Texture = Load("backdrops/soot");
		_chest.Texture = Load("chest/standard");
		_shoulders.Texture = Load($"shoulders/{Parts.Shoulders.ToString().ToLowerInvariant()}");
		_ears.Texture = Load($"ears/{Parts.Ears.ToString().ToLowerInvariant()}");
		_antenna.Texture = Parts.Antenna == RobotAntenna.None
			? null : Load($"antennas/{Parts.Antenna.ToString().ToLowerInvariant()}");
		var screen = Parts.Screen.ToString().ToLowerInvariant();
		_bezel.Texture = Load($"screens/{screen}_bezel");
		_screen.Texture = Load($"screens/{screen}_glass");
		_eyes.Texture = Load($"faces/{Parts.Eyes.ToString().ToLowerInvariant()}_eyes");
		_mouth.Texture = Load($"faces/{Parts.Mouth.ToString().ToLowerInvariant()}_mouth");
		_rust.Texture = Parts.Rust == RobotRust.None
			? null : Load($"rust/{Parts.Rust.ToString().ToLowerInvariant()}");
		_cracks.Texture = Parts.Cracks == RobotCracks.None
			? null : Load($"cracks/{Parts.Cracks.ToString().ToLowerInvariant()}");
		_tape.Texture = Parts.Tape == RobotTape.None
			? null : Load($"tape/{Parts.Tape.ToString().ToLowerInvariant()}");
	}

	private void UpdateColors()
	{
		_chest.SelfModulate = Palette.Chest;
		_shoulders.SelfModulate = Palette.Shoulders;
		_ears.SelfModulate = Palette.Ears;
		_antenna.SelfModulate = Palette.Antenna;
		_bezel.SelfModulate = Palette.Casing;
		_screen.SelfModulate = Palette.Screen;
		_eyes.SelfModulate = Palette.Glyph;
		_mouth.SelfModulate = Palette.Glyph;
	}

	private void UpdateVisibility()
	{
		_backdrop.Visible = Layer == RobotPortraitLayer.All;
		_chest.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Chest;
		_shoulders.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Shoulders;
		_ears.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Ears;
		_antenna.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Antenna;
		_bezel.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Screen;
		_screen.Visible = _bezel.Visible;
		_eyes.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Eyes;
		_mouth.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Mouth;
		_rust.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Rust;
		_cracks.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Cracks;
		_tape.Visible = Layer is RobotPortraitLayer.All or RobotPortraitLayer.Tape;
	}

	private void LayoutLayers()
	{
		var scale = Mathf.Min(Size.X, Size.Y) / CanvasSize;
		var size = Vector2.One * CanvasSize * scale;
		var origin = (Size - size) / 2f;
		foreach (var image in new[] { _backdrop, _chest, _shoulders, _ears, _antenna, _bezel, _screen, _eyes, _mouth, _rust, _cracks, _tape })
		{
			image.Position = origin;
			image.Size = size;
			image.Scale = Vector2.One;
			image.PivotOffset = Vector2.Zero;
			image.Rotation = 0f;
		}

		// Whole-cell offsets keep the triangular face aligned with the authored LED grid.
		var triangle = Parts.Screen == RobotScreen.Triangle;
		_eyes.Position += new Vector2(0, triangle ? 24f * scale : 0);
		_eyes.PivotOffset = new Vector2(160, 132) * scale;
		_eyes.Scale = new Vector2(1f, Mathf.Max(.04f, EyeOpenness));
		_mouth.Position += new Vector2(0, triangle ? 24f * scale : 0);
		if (Parts.CrackPlacement is { } crack)
			PlaceOverlay(_cracks, crack, Parts.Cracks == RobotCracks.Branching ? new(162, 104) : new(163, 98), origin, scale);
		if (Parts.TapePlacement is { } tape)
			PlaceOverlay(_tape, tape, Parts.Tape == RobotTape.Crown ? new(157, 58) : new(103, 287), origin, scale);
	}

	private static void PlaceOverlay(TextureRect image, RobotWearPlacement placement, Vector2 assetCenter,
		Vector2 origin, float canvasScale)
	{
		image.PivotOffset = assetCenter * canvasScale;
		image.Position = origin + (new Vector2(placement.X, placement.Y) - assetCenter) * canvasScale;
		image.Scale = Vector2.One * placement.Scale;
		image.RotationDegrees = placement.RotationDegrees;
	}

	private static Texture2D Load(string asset)
	{
		var path = $"{AssetRoot}/{asset}.svg";
		return GD.Load<Texture2D>(path)
			?? throw new InvalidOperationException($"Could not load robot portrait asset '{path}'.");
	}

	private static void Validate(RobotPortraitParts parts)
	{
		if (!Enum.IsDefined(parts.Eyes) || !Enum.IsDefined(parts.Mouth) || !Enum.IsDefined(parts.Screen) ||
			!Enum.IsDefined(parts.Ears) || !Enum.IsDefined(parts.Antenna) ||
			!Enum.IsDefined(parts.Shoulders) || !Enum.IsDefined(parts.Cracks) ||
			!Enum.IsDefined(parts.Tape) || !Enum.IsDefined(parts.Rust))
			throw new ArgumentException("Robot portrait contains an unknown part ID.", nameof(parts));
		foreach (var placement in new[] { parts.CrackPlacement, parts.TapePlacement })
			if (placement is { } p && (!Enum.IsDefined(p.Location) ||
				!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.RotationDegrees) ||
				!float.IsFinite(p.Scale) || p.Scale <= 0))
				throw new ArgumentException("Robot portrait contains an invalid wear placement.", nameof(parts));
	}
}
