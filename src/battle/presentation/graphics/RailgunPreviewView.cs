using Godot;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class RailgunPreviewView : Node3D
{
	private const int RingSides = 18;
	private const float AimStrength = 0.85f;
	private const float HoverStrength = 1.35f;

	private static readonly Color Tint = new(0.55f, 0.82f, 1f, 0.42f);

	private static readonly WeaponPreviewMesh.Section[] Sections =
	[
		// Fade in just beyond the ship's nose.
		new(0.5f, 0.12f, 0.00f),
		new(0.9f, 0.24f, 0.70f),

		// Long, narrow railgun corridor.
		new(3.0f, 0.28f, 0.95f),
		new(6.0f, 0.32f, 0.95f),
		new(CombatConfig.RailgunLineLength, 0.38f, 0.90f),

		// Final spreading part of the burst.
		new(CombatConfig.RailgunLineLength + 1, 1.15f, 0.70f),
		new(
			CombatConfig.RailgunLineLength
			+ CombatConfig.RailgunPyramidRange
			+ 0.7f,
			CombatConfig.RailgunPyramidRange + 0.5f,
			0.00f),
	];

	private MeshInstance3D? _plume;
	private ShaderMaterial? _material;

	public void Build()
	{
		_material = WeaponPreviewMaterials.CreateDotted(Tint);

		_plume = new MeshInstance3D
		{
			Name = "RailgunPlume",
			Mesh = WeaponPreviewMesh.CreatePlume(Sections, RingSides),
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(_plume);

		AddChild(_plume);
		Visible = false;
	}

	public bool PickHovered(Camera3D camera, Vector2 screenPos) =>
		_plume is { Visible: true } && PreviewPick.NearNode(camera, screenPos, _plume);

	public void ApplyFrame(PresentationFrame frame)
	{
		var aiming = frame.ShowWeaponPreviews && frame.Mode == EPlayerMode.Railgun;
		var cemented = frame.ShowWeaponPreviews && frame.QueuedWeapon.Railgun;
		var shouldShow = aiming || cemented;

		Visible = shouldShow;
		if (!shouldShow || _material is null)
			return;

		var state = frame.FocusState.ToState();

		Position = WorldMapping.ToWorld(state.Position);

		// The plume mesh points along local +Z, matching UnitView/ShipMesh.
		Basis = new Basis(
			ToVector3(state.Starboard),
			ToVector3(state.Dorsal),
			ToVector3(state.Fore));

		if (aiming)
		{
			WeaponPreviewMaterials.ApplyAim(
				_material,
				Tint,
				frame.RailgunHovered ? HoverStrength : AimStrength);
			return;
		}

		WeaponPreviewMaterials.ApplyCemented(_material);
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
