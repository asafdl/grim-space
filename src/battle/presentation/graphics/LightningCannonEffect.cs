using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class LightningCannonEffect : Node3D
{
	private const string BoltPath = "res://assets/models/abilities/lightning_cannon_bolts_bold.glb";
	internal const float StrikeSeconds = 0.065f;
	private const float LifetimeSeconds = 0.22f;
	private const float BeamThicknessScale = 1.22f * 1.35f;
	private static PackedScene? _bolts;
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
		int lineLength,
		int pyramidRange,
		float strikeSeconds = StrikeSeconds,
		float lifetimeSeconds = LifetimeSeconds)
	{
		if (cellSize <= 0 || lineLength < 1 || pyramidRange < 0)
			throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size and line length must be positive; pyramid range cannot be negative.");

		var fore = forward.Normalized();
		var up = (dorsal - fore * dorsal.Dot(fore)).Normalized();
		if (fore.LengthSquared() < 0.9f || up.LengthSquared() < 0.9f)
			throw new ArgumentException("Forward and dorsal must be nonparallel directions.");

		_bolts ??= GD.Load<PackedScene>(BoltPath)
			?? throw new InvalidOperationException($"Could not load lightning VFX '{BoltPath}'.");
		var root = _bolts.Instantiate<Node3D>();
		var effect = new LightningCannonEffect
		{
			Name = "LightningCannonEffect",
			_strikeSeconds = strikeSeconds,
			_lifetimeSeconds = lifetimeSeconds,
			_chargeSoundPosition = origin,
			_soundParent = parent,
		};
		try
		{
			var meshes = root.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>().ToDictionary(mesh => mesh.Name.ToString());
			var right = up.Cross(fore).Normalized();
			var orientation = new Basis(fore, right, up);
			var beamThickness = cellSize * BeamThicknessScale;
			foreach (var name in new[] { "MainBoltA", "MainBoltB", "MainBoltC" })
			{
				effect.AddPiece(FindMesh(name), origin, orientation,
					new Vector3(lineLength * cellSize, beamThickness, beamThickness), 1f);
			}

			if (pyramidRange > 0)
			{
				// TerminalForks: short +X trunk, branches in local Y/Z — one aligned copy at the beam tip.
				var rng = new RandomNumberGenerator { Seed = GD.Randi() };
				const float overlapCells = 0.35f;
				var roll = rng.RandfRange(-0.22f, 0.22f);
				var forkOrientation = orientation.Rotated(fore, roll);
				var junction = origin + fore * ((lineLength - overlapCells) * cellSize);
				var depthScale = (pyramidRange + overlapCells) * rng.RandfRange(0.96f, 1.04f);
				var spreadScale = pyramidRange * rng.RandfRange(0.95f, 1.08f);
				effect.AddPiece(
					FindMesh("TerminalForks"),
					junction,
					forkOrientation,
					new Vector3(depthScale * cellSize, spreadScale * cellSize, spreadScale * cellSize),
					rng.RandfRange(0.78f, 0.84f));
			}

			parent.AddChild(effect);
			return effect;

			Mesh FindMesh(string name) =>
				meshes.TryGetValue(name, out var source) && source.Mesh is { } mesh
					? mesh
					: throw new InvalidOperationException($"Lightning VFX '{BoltPath}' has no mesh '{name}'.");
		}
		finally
		{
			root.Free();
			if (effect.GetParent() is null)
				effect.Free();
		}
	}

	private void AddPiece(Mesh mesh, Vector3 position, Basis orientation, Vector3 scale, float opacity)
	{
		var piece = new MeshInstance3D
		{
			Mesh = mesh,
			Position = position,
			Basis = orientation,
			Scale = scale,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
				AlbedoColor = new Color(0.62f, 0.72f, 1f, opacity),
				EmissionEnabled = true,
				Emission = new Color(0.62f, 0.72f, 1f),
				EmissionEnergyMultiplier = 2.3f,
			},
		};
		AddChild(piece);
		_pieces.Add(piece);
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
