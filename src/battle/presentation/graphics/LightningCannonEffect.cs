using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class LightningCannonEffect : Node3D
{
	private const string SquigglePath =
		"res://assets/vfx/lightning_cannon_squiggle.svg";
	internal const float StrikeSeconds = 0.065f;
	private const float LifetimeSeconds = 0.22f;
	private const float SquiggleAspectRatio = 1220.9414f / 690.4336f;
	private const float ExtrusionDepthCells = 0.12f;
	private const int ExtrusionLayers = 7;
	private static Texture2D? _squiggle;
	private readonly List<MeshInstance3D> _pieces = [];
	private float _elapsed;
	private float _strikeSeconds = StrikeSeconds;
	private float _lifetimeSeconds = LifetimeSeconds;
	private Vector3 _chargeSoundPosition;
	private Node3D _soundParent = null!;

	public static LightningCannonEffect Fire(
		Node3D parent,
		Vector3 origin,
		Vector3 forward,
		Vector3 dorsal,
		float cellSize,
		float lineLength,
		int pyramidRange,
		float strikeSeconds = StrikeSeconds,
		float lifetimeSeconds = LifetimeSeconds)
	{
		if (cellSize <= 0 || lineLength <= 0f || pyramidRange < 0)
			throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size and line length must be positive; pyramid range cannot be negative.");

		var fore = forward.Normalized();
		var up = (dorsal - fore * dorsal.Dot(fore)).Normalized();
		if (fore.LengthSquared() < 0.9f || up.LengthSquared() < 0.9f)
			throw new ArgumentException("Forward and dorsal must be nonparallel directions.");

		_squiggle ??= GD.Load<Texture2D>(SquigglePath)
			?? throw new InvalidOperationException(
				$"Could not load lightning squiggle '{SquigglePath}'.");
		var effect = new LightningCannonEffect
		{
			Name = "LightningCannonEffect",
			_strikeSeconds = strikeSeconds,
			_lifetimeSeconds = lifetimeSeconds,
			_chargeSoundPosition = origin,
			_soundParent = parent,
		};
		var right = up.Cross(fore).Normalized();
		var reach = (lineLength + pyramidRange) * cellSize;
		var spread = Mathf.Max(
			Mathf.Max(pyramidRange * cellSize, reach * 0.2f),
			cellSize);
		var launchOffset = fore * (cellSize * 0.25f);

		effect.AddExtrudedSquiggle(
			origin + launchOffset,
			origin + fore * reach + right * spread,
			right,
			cellSize,
			0.82f);
		effect.AddExtrudedSquiggle(
			origin + launchOffset - right * (cellSize * 0.12f),
			origin + fore * (reach * 1.04f) - right * (spread * 0.9f),
			right,
			cellSize,
			0.72f);
		effect.AddExtrudedSquiggle(
			origin + launchOffset + right * (cellSize * 0.1f),
			origin + fore * (reach * 0.94f) + right * (spread * 0.1f),
			right,
			cellSize,
			0.66f);

		parent.AddChild(effect);
		return effect;
	}

	private void AddExtrudedSquiggle(
		Vector3 from,
		Vector3 to,
		Vector3 heightDirection,
		float cellSize,
		float scale)
	{
		var direction = from.DirectionTo(to);
		var height = (
			heightDirection
			- direction * heightDirection.Dot(direction)
		).Normalized();
		var normal = direction.Cross(height).Normalized();
		var length = from.DistanceTo(to);
		var size = new Vector2(
			length,
			length / SquiggleAspectRatio * scale);
		var depth = ExtrusionDepthCells * cellSize * scale;
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			AlbedoTexture = _squiggle,
		};

		for (var layer = 0; layer < ExtrusionLayers; layer++)
		{
			var depthProgress = layer / (float)(ExtrusionLayers - 1) - 0.5f;
			var piece = new MeshInstance3D
			{
				Name = "LightningSquiggle",
				Mesh = new QuadMesh { Size = size },
				Position = from.Lerp(to, 0.5f)
					+ normal * (depthProgress * depth),
				Basis = new Basis(direction, height, normal),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
				MaterialOverride = material,
			};
			PresentationLayers.MarkWorld(piece);
			AddChild(piece);
			_pieces.Add(piece);
		}
	}

	public override void _Ready() =>
		PresentationSfx.PlayWorldOneShot(
			_soundParent,
			_chargeSoundPosition,
			PresentationSfx.LightningCannonFirePath);

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;
		if (_elapsed >= _lifetimeSeconds)
		{
			QueueFree();
			return;
		}

		var alpha = _elapsed < _strikeSeconds
			? 1f
			: 0.3f * (1f - (_elapsed - _strikeSeconds) / (_lifetimeSeconds - _strikeSeconds));
		foreach (var piece in _pieces)
			piece.Transparency = 1f - alpha;
	}
}
