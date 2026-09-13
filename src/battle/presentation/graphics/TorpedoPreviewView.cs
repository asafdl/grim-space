using Godot;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class TorpedoPreviewView : Node3D
{
	private const float AimMountStrength = 0.95f;
	private const float HoverMountStrength = 1.35f;

	private static readonly Color MountTint = new(0.25f, 0.85f, 0.95f, 0.55f);

	private static readonly Color[] TurnTints =
	[
		new(0.15f, 0.90f, 1.00f, 0.16f),
		new(0.25f, 0.55f, 1.00f, 0.10f),
		new(0.65f, 0.30f, 1.00f, 0.05f),
	];

	private readonly List<MeshInstance3D> _activeMarkers = [];
	private readonly Queue<MeshInstance3D> _freeMarkers = [];
	private SphereMesh? _mountMesh;
	private ShaderMaterial? _mountMaterial;
	private TurnVolumeWireframe? _aimTravel;
	private PresentationFrame? _frame;

	public void Build()
	{
		_mountMesh = new SphereMesh
		{
			Radius = WorldMapping.CellSize * 0.42f,
			Height = WorldMapping.CellSize * 0.84f,
		};
		_mountMaterial = WeaponPreviewMaterials.CreateDotted(MountTint);

		_aimTravel = new TurnVolumeWireframe("TorpedoAimTurn", TurnTints);
		foreach (var instance in _aimTravel.Instances)
			AddChild(instance);

		Visible = false;
	}

	public ESpatialOrientation? PickMountedOn(Camera3D camera, Vector2 screenPosition)
	{
		if (_frame is null || _frame.Mode != EPlayerMode.Torpedo || !_frame.ShowWeaponPreviews)
			return null;

		var ship = _frame.FocusState.ToState();
		var cells = _frame.Weapons.TorpedoMounts.ToDictionary(
			mountedOn => TorpedoMount.LaunchPose(ship, mountedOn).Position,
			mountedOn => mountedOn);
		return GridPick.PickFromSet(camera, screenPosition, cells.Keys.ToHashSet()) is Coord cell
			? cells[cell]
			: null;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		_frame = frame;
		var aiming = frame.ShowWeaponPreviews && frame.Mode == EPlayerMode.Torpedo;
		Visible = aiming;

		ReleaseMarkers();
		_aimTravel?.Apply(aiming ? frame.TorpedoPreviews.Aim : null);
		if (!Visible
			|| _mountMesh is null
			|| _mountMaterial is null)
		{
			return;
		}

		if (aiming)
		{
			var effectiveMount = frame.StagedMountedOn ?? frame.TorpedoHoverMountedOn;
			WeaponPreviewMaterials.ApplyAim(
				_mountMaterial,
				MountTint,
				effectiveMount is not null ? HoverMountStrength : AimMountStrength);

			var ship = frame.FocusState.ToState();
			foreach (var mountedOn in frame.Weapons.TorpedoMounts)
			{
				var (position, _, _) = TorpedoMount.LaunchPose(ship, mountedOn);
				Place(_mountMaterial, position);
			}
		}
	}

	private void Place(ShaderMaterial material, Coord cell)
	{
		var marker = Acquire(material);
		marker.Position = WorldMapping.ToWorld(cell);
		marker.Visible = true;
		_activeMarkers.Add(marker);
	}

	private MeshInstance3D Acquire(ShaderMaterial material)
	{
		if (_freeMarkers.Count > 0)
		{
			var reused = _freeMarkers.Dequeue();
			reused.MaterialOverride = material;
			return reused;
		}

		var marker = new MeshInstance3D
		{
			Mesh = _mountMesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(marker);
		AddChild(marker);
		return marker;
	}

	private void ReleaseMarkers()
	{
		foreach (var marker in _activeMarkers)
		{
			marker.Visible = false;
			_freeMarkers.Enqueue(marker);
		}

		_activeMarkers.Clear();
	}
}
