using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class LightningCannonEffect : Node3D
{
	private const string ModelPath = "res://assets/vfx/zap-godot.glb";
	private const string BoltsNodeName = "LightningBolts";
	private const string TextNodeName = "ZapText";
	private const float GrowthSeconds = 0.2f;
	private const float HoldSeconds = 0.3f;
	private const float BoltFadeSeconds = 0.2f;
	private const float TextAppearSeconds = 0.16f;
	private const float TextFadeSeconds = 0.2f;
	internal const float StrikeSeconds = GrowthSeconds;
	private const float LifetimeSeconds = 0.8f;
	private static PackedScene? _model;
	private static Shader? _revealShader;
	private readonly List<MeshInstance3D> _boltPieces = [];
	private readonly List<ShaderMaterial> _boltMaterials = [];
	private readonly List<MeshInstance3D> _textPieces = [];
	private float _boltMinimumX;
	private float _boltMaximumX;
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

		_model ??= GD.Load<PackedScene>(ModelPath)
			?? throw new InvalidOperationException(
				$"Could not load lightning model '{ModelPath}'.");
		var effect = new LightningCannonEffect
		{
			Name = "LightningCannonEffect",
			_strikeSeconds = strikeSeconds,
			_lifetimeSeconds = lifetimeSeconds,
			_chargeSoundPosition = origin,
			_soundParent = parent,
		};
		var reach = (lineLength + pyramidRange) * cellSize;
		var launchOffset = fore * (cellSize * 0.25f);

		effect.AddModel(origin + launchOffset, fore, up, reach);

		parent.AddChild(effect);
		return effect;
	}

	private void AddModel(
		Vector3 origin,
		Vector3 forward,
		Vector3 dorsal,
		float reach)
	{
		var model = _model!.Instantiate<Node3D>();
		AddChild(model);
		try
		{
			var bounds = FindBounds(model);
			if (bounds.Size.X <= 0f)
				throw new InvalidOperationException(
					$"Lightning model '{ModelPath}' has no longitudinal extent.");

			var scale = reach / bounds.Size.X;
			var localForward = -forward;
			var lateral = localForward.Cross(dorsal).Normalized();
			var orientation = new Basis(localForward, dorsal, lateral);
			var anchor = new Vector3(
				bounds.End.X,
				bounds.GetCenter().Y,
				bounds.GetCenter().Z);
			model.Transform = new Transform3D(
				orientation.Scaled(Vector3.One * scale),
				origin - orientation * (anchor * scale));
		}
		catch
		{
			RemoveChild(model);
			model.Free();
			throw;
		}
	}

	private Aabb FindBounds(Node3D model)
	{
		var bounds = default(Aabb);
		var foundBolts = false;
		var foundText = false;
		foreach (var node in model.FindChildren("*", "MeshInstance3D", true, false))
		{
			if (node is not MeshInstance3D { Mesh: { } mesh } piece)
				continue;

			var transform = Transform3D.Identity;
			for (Node3D? current = piece; current is not null; current = current.GetParent() as Node3D)
			{
				transform = current.Transform * transform;
				if (current == model)
					break;
			}

			switch (piece.Name.ToString())
			{
				case BoltsNodeName:
					var pieceBounds = transform * mesh.GetAabb();
					bounds = foundBolts
						? bounds.Merge(pieceBounds)
						: pieceBounds;
					foundBolts = true;
					var localBounds = mesh.GetAabb();
					_boltMinimumX = localBounds.Position.X;
					_boltMaximumX = localBounds.End.X;
					PrepareRevealMaterials(piece);
					_boltPieces.Add(piece);
					break;
				case TextNodeName:
					PrepareBillboardMaterials(piece);
					foundText = true;
					piece.Transparency = 1f;
					_textPieces.Add(piece);
					break;
			}

			PresentationLayers.MarkWorld(piece);
			piece.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}

		if (!foundBolts)
			throw new InvalidOperationException(
				$"Lightning model '{ModelPath}' has no '{BoltsNodeName}' mesh.");
		if (!foundText)
			throw new InvalidOperationException(
				$"Lightning model '{ModelPath}' has no '{TextNodeName}' mesh.");
		return bounds;
	}

	private void PrepareRevealMaterials(MeshInstance3D bolts)
	{
		_revealShader ??= new Shader
		{
			Code =
				"""
				shader_type spatial;
				render_mode unshaded, cull_disabled;

				uniform vec4 color : source_color;
				uniform float emission_energy = 1.0;
				uniform float reveal_cutoff = 0.0;
				varying float local_x;

				void vertex() {
					local_x = VERTEX.x;
				}

				void fragment() {
					if (local_x < reveal_cutoff) {
						discard;
					}
					ALBEDO = color.rgb;
					EMISSION = color.rgb * emission_energy;
					ALPHA = color.a;
				}
				"""
		};

		for (var surface = 0; surface < bolts.Mesh!.GetSurfaceCount(); surface++)
		{
			var source = bolts.GetActiveMaterial(surface) as StandardMaterial3D
				?? throw new InvalidOperationException(
					$"Lightning bolt surface {surface} has no standard material.");
			var material = new ShaderMaterial { Shader = _revealShader };
			var color = source.EmissionEnabled
				? source.Emission
				: source.AlbedoColor;
			material.SetShaderParameter("color", color);
			material.SetShaderParameter(
				"emission_energy",
				source.EmissionEnabled
					? source.EmissionEnergyMultiplier
					: 1f);
			material.SetShaderParameter(
				"reveal_cutoff",
				_boltMaximumX);
			bolts.SetSurfaceOverrideMaterial(surface, material);
			_boltMaterials.Add(material);
		}
	}

	private static void PrepareBillboardMaterials(MeshInstance3D text)
	{
		for (var surface = 0; surface < text.Mesh!.GetSurfaceCount(); surface++)
		{
			var source = text.GetActiveMaterial(surface) as BaseMaterial3D
				?? throw new InvalidOperationException(
					$"Lightning text surface {surface} has no 3D material.");
			var material = (BaseMaterial3D)source.Duplicate();
			material.BillboardMode =
				BaseMaterial3D.BillboardModeEnum.Enabled;
			material.BillboardKeepScale = true;
			text.SetSurfaceOverrideMaterial(surface, material);
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

		var growth = Mathf.Clamp(_elapsed / _strikeSeconds, 0f, 1f);
		var easedGrowth = growth * growth * (3f - 2f * growth);
		var revealCutoff = Mathf.Lerp(
			_boltMaximumX,
			_boltMinimumX,
			easedGrowth);
		foreach (var material in _boltMaterials)
			material.SetShaderParameter(
				"reveal_cutoff",
				revealCutoff);

		var boltFadeStart = GrowthSeconds + HoldSeconds;
		var boltAlpha = _elapsed < boltFadeStart
			? 1f
			: 1f - Mathf.Clamp(
				(_elapsed - boltFadeStart) / BoltFadeSeconds,
				0f,
				1f);
		SetAlpha(_boltPieces, boltAlpha);

		var textFadeStart = _lifetimeSeconds - TextFadeSeconds;
		var textAlpha = _elapsed < TextAppearSeconds
			? 0f
			: _elapsed < GrowthSeconds
				? (_elapsed - TextAppearSeconds)
					/ (GrowthSeconds - TextAppearSeconds)
				: _elapsed < textFadeStart
					? 1f
					: 1f - (_elapsed - textFadeStart)
						/ TextFadeSeconds;
		SetAlpha(_textPieces, textAlpha);
	}

	private static void SetAlpha(
		IEnumerable<MeshInstance3D> pieces,
		float alpha)
	{
		foreach (var piece in pieces)
			piece.Transparency = 1f - alpha;
	}
}
