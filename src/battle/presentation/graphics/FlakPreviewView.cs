using Godot;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class FlakPreviewView : Node3D
{
	private const int RingSides = 16;
	private const float AimStrength = 0.85f;
	private const float HoverStrength = 1.35f;

	private static readonly Color PortTint = new(0.95f, 0.55f, 0.18f, 0.40f);
	private static readonly Color StarboardTint = new(0.98f, 0.78f, 0.22f, 0.40f);

	// Flak steps outward along ±port: cells at distance 1..Range+1 with expanding radius.
	private static readonly WeaponPreviewMesh.Section[] Sections =
	[
		new(0.35f, 0.12f, 0.00f),
		new(1.0f, 0.70f, 0.85f),
		new(2.0f, 1.25f, 0.90f),
		new(CombatConfig.FlakRange + 1f, 1.85f, 0.75f),
		new(CombatConfig.FlakRange + 1.6f, 2.25f, 0.00f),
	];

	private MeshInstance3D? _port;
	private MeshInstance3D? _starboard;
	private ShaderMaterial? _portMaterial;
	private ShaderMaterial? _starboardMaterial;
	private PresentationFrame? _frame;

	public void Build()
	{
		var mesh = WeaponPreviewMesh.CreatePlume(Sections, RingSides);

		_portMaterial = WeaponPreviewMaterials.CreateDotted(PortTint);
		_starboardMaterial = WeaponPreviewMaterials.CreateDotted(StarboardTint);

		_port = CreatePlume("FlakPort", mesh, _portMaterial);
		_starboard = CreatePlume("FlakStarboard", mesh, _starboardMaterial);

		AddChild(_port);
		AddChild(_starboard);
		Visible = false;
	}

	public ESpatialOrientation? PickMountedOn(Camera3D camera, Vector2 screenPos)
	{
		if (_frame is null || _frame.Mode != EPlayerMode.Flak || !_frame.ShowWeaponPreviews)
			return null;

		var weapons = _frame.Weapons;
		var bodyFrame = BodyFrame.From(_frame.FocusState.ToState());
		var cells = new Dictionary<Coord, ESpatialOrientation>();

		if (weapons.PortFlak)
			AddBurstCells(cells, bodyFrame, ESpatialOrientation.Port);

		if (weapons.StarboardFlak)
			AddBurstCells(cells, bodyFrame, ESpatialOrientation.Starboard);

		return GridPick.PickFromSet(camera, screenPos, cells.Keys.ToHashSet()) is Coord cell
			? cells[cell]
			: null;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		_frame = frame;
		var queued = frame.QueuedWeapon;
		var aiming = frame.ShowWeaponPreviews && frame.Mode == EPlayerMode.Flak;
		var showPort = aiming && frame.Weapons.PortFlak
			|| frame.ShowWeaponPreviews && queued.FlakMountedOn == ESpatialOrientation.Port;
		var showStarboard = aiming && frame.Weapons.StarboardFlak
			|| frame.ShowWeaponPreviews && queued.FlakMountedOn == ESpatialOrientation.Starboard;
		var shouldShow = showPort || showStarboard;

		Visible = shouldShow;
		if (!shouldShow || _port is null || _starboard is null)
			return;

		var state = frame.FocusState.ToState();
		Position = WorldMapping.ToWorld(state.Position);

		var starboard = ToVector3(state.Starboard);
		var dorsal = ToVector3(state.Dorsal);
		var fore = ToVector3(state.Fore);

		// Mesh +Z is fire direction: port = -starboard, starboard = +starboard.
		_port.Basis = new Basis(fore, dorsal, -starboard);
		_starboard.Basis = new Basis(-fore, dorsal, starboard);
		_port.Visible = showPort;
		_starboard.Visible = showStarboard;

		if (aiming)
		{
			WeaponPreviewMaterials.ApplyAim(
				_portMaterial!,
				PortTint,
				Strength(showPort, frame.FlakHoverMountedOn == ESpatialOrientation.Port));
			WeaponPreviewMaterials.ApplyAim(
				_starboardMaterial!,
				StarboardTint,
				Strength(showStarboard, frame.FlakHoverMountedOn == ESpatialOrientation.Starboard));
			return;
		}

		if (showPort)
			WeaponPreviewMaterials.ApplyCemented(_portMaterial!);
		if (showStarboard)
			WeaponPreviewMaterials.ApplyCemented(_starboardMaterial!);
	}

	private static void AddBurstCells(
		Dictionary<Coord, ESpatialOrientation> cells,
		BodyFrame frame,
		ESpatialOrientation mountedOn)
	{
		foreach (var cell in WeaponBursts.FlakBurstCells(frame, mountedOn, static _ => true))
			cells[cell] = mountedOn;
	}

	private static float Strength(bool available, bool hovered) =>
		!available ? 0f : hovered ? HoverStrength : AimStrength;

	private static MeshInstance3D CreatePlume(
		string name,
		ArrayMesh mesh,
		ShaderMaterial material)
	{
		var instance = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(instance);
		return instance;
	}

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
