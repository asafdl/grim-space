using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class TorpedoPreviewView : Node3D
{
	private const float AimMountStrength = 0.95f;
	private const float HoverMountStrength = 1.35f;
	private const float EnvelopeStrength = 1.05f;

	private static readonly Color MountTint = new(0.25f, 0.85f, 0.95f, 0.55f);

	private static readonly Color[] EnvelopeTints =
	[
		new(0.18f, 0.75f, 0.92f, 0.36f),
		new(0.14f, 0.55f, 0.78f, 0.24f),
		new(0.10f, 0.38f, 0.58f, 0.16f),
	];

	private readonly List<MeshInstance3D> _active = [];
	private readonly Queue<MeshInstance3D> _free = [];

	private SphereMesh? _mountMesh;
	private SphereMesh? _envelopeMesh;
	private ShaderMaterial? _mountMaterial;
	private ShaderMaterial[]? _envelopeMaterials;
	private ShaderMaterial? _cementedMaterial;

	public void Build()
	{
		_mountMesh = new SphereMesh
		{
			Radius = WorldMapping.CellSize * 0.42f,
			Height = WorldMapping.CellSize * 0.84f,
		};
		_envelopeMesh = new SphereMesh
		{
			Radius = WorldMapping.CellSize * 0.34f,
			Height = WorldMapping.CellSize * 0.68f,
		};
		_mountMaterial = WeaponPreviewMaterials.CreateDotted(MountTint);
		_envelopeMaterials = EnvelopeTints
			.Select(WeaponPreviewMaterials.CreateDotted)
			.ToArray();
		_cementedMaterial = WeaponPreviewMaterials.CreateDotted(WeaponPreviewMaterials.CementedTint);
		WeaponPreviewMaterials.ApplyCemented(_cementedMaterial);
		Visible = false;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		var aiming =
			frame.Mode == EPlayerMode.Torpedo
			&& frame.TorpedoMountCells.Count > 0;
		var cemented = frame.CommittedTorpedoMountCell is Coord;
		var shouldShow = (aiming || cemented) && !frame.ShowOutcomeOverlay;

		Visible = shouldShow;
		ReleaseActive();
		if (!shouldShow
			|| _mountMesh is null
			|| _envelopeMesh is null
			|| _mountMaterial is null
			|| _envelopeMaterials is null
			|| _cementedMaterial is null)
		{
			return;
		}

		if (aiming)
		{
			var hovered = frame.TorpedoEnvelopeLayers.Count > 0;
			WeaponPreviewMaterials.ApplyAim(
				_mountMaterial,
				MountTint,
				hovered ? HoverMountStrength : AimMountStrength);

			foreach (var cell in frame.TorpedoMountCells)
				Place(_mountMesh, _mountMaterial, cell);

			PlaceAimEnvelope(frame.TorpedoEnvelopeLayers, frame.TorpedoMountCells);
			return;
		}

		Place(_mountMesh, _cementedMaterial, frame.CommittedTorpedoMountCell!.Value);
		PlaceCementedEnvelope(
			frame.CommittedTorpedoEnvelopeLayers,
			new HashSet<Coord> { frame.CommittedTorpedoMountCell.Value });
	}

	private void PlaceAimEnvelope(
		IReadOnlyList<IReadOnlySet<Coord>> layers,
		IReadOnlySet<Coord> skipCells)
	{
		for (var layer = layers.Count - 1; layer >= 0; layer--)
		{
			var material = EnvelopeMaterial(layer);
			var tintIndex = layer < EnvelopeTints.Length ? layer : EnvelopeTints.Length - 1;
			WeaponPreviewMaterials.ApplyAim(material, EnvelopeTints[tintIndex], EnvelopeStrength);
			foreach (var cell in layers[layer])
			{
				if (skipCells.Contains(cell))
					continue;

				Place(_envelopeMesh!, material, cell);
			}
		}
	}

	private void PlaceCementedEnvelope(
		IReadOnlyList<IReadOnlySet<Coord>> layers,
		IReadOnlySet<Coord> skipCells)
	{
		for (var layer = layers.Count - 1; layer >= 0; layer--)
		{
			foreach (var cell in layers[layer])
			{
				if (skipCells.Contains(cell))
					continue;

				Place(_envelopeMesh!, _cementedMaterial!, cell);
			}
		}
	}

	private ShaderMaterial EnvelopeMaterial(int layer)
	{
		var materials = _envelopeMaterials!;
		return layer < materials.Length ? materials[layer] : materials[^1];
	}

	private void Place(SphereMesh mesh, ShaderMaterial material, Coord cell)
	{
		var marker = Acquire(mesh, material);
		marker.Position = WorldMapping.ToWorld(cell);
		marker.Visible = true;
		_active.Add(marker);
	}

	private MeshInstance3D Acquire(SphereMesh mesh, ShaderMaterial material)
	{
		if (_free.Count > 0)
		{
			var reused = _free.Dequeue();
			reused.Mesh = mesh;
			reused.MaterialOverride = material;
			return reused;
		}

		var marker = new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(marker);
		AddChild(marker);
		return marker;
	}

	private void ReleaseActive()
	{
		foreach (var marker in _active)
		{
			marker.Visible = false;
			_free.Enqueue(marker);
		}

		_active.Clear();
	}
}
