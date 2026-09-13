using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class FlakPreviewView : Node3D
{
	private const float AimStrength = 0.85f;
	private const float HoverStrength = 1.35f;

	private static readonly Color PortTint = new(0.95f, 0.55f, 0.18f, 0.40f);
	private static readonly Color StarboardTint = new(0.98f, 0.78f, 0.22f, 0.40f);

	private CellVolumeWireframeSlot _aimPort = null!;
	private CellVolumeWireframeSlot _aimStarboard = null!;
	private CellVolumeWireframeSlot _queued = null!;
	private StandardMaterial3D _portMaterial = null!;
	private StandardMaterial3D _starboardMaterial = null!;
	private PresentationFrame? _frame;

	public void Build()
	{
		_portMaterial = WeaponPreviewMaterials.CreateWireframe(PortTint);
		_starboardMaterial = WeaponPreviewMaterials.CreateWireframe(StarboardTint);
		var queuedMaterial = WeaponPreviewMaterials.CreateWireframe(
			WeaponPreviewMaterials.CementedTint);
		_aimPort = new CellVolumeWireframeSlot("FlakPortAim", _portMaterial);
		_aimStarboard = new CellVolumeWireframeSlot("FlakStarboardAim", _starboardMaterial);
		_queued = new CellVolumeWireframeSlot("FlakQueued", queuedMaterial);
		AddChild(_aimPort.Instance);
		AddChild(_aimStarboard.Instance);
		AddChild(_queued.Instance);
		Visible = false;
	}

	public ESpatialOrientation? PickMountedOn(Camera3D camera, Vector2 screenPos)
	{
		if (_frame is null || _frame.Mode != EPlayerMode.Flak || !_frame.ShowWeaponPreviews)
			return null;

		var cells = new Dictionary<Coord, ESpatialOrientation>();

		foreach (var preview in _frame.AreaActions.Aim)
		{
			if (preview.Action is not FlakAction flak)
				continue;
			foreach (var burstCell in preview.Volume.Cells)
				cells[burstCell] = flak.MountedOn;
		}

		return GridPick.PickFromSet(camera, screenPos, cells.Keys.ToHashSet()) is Coord cell
			? cells[cell]
			: null;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		_frame = frame;
		var aiming = frame.ShowWeaponPreviews && frame.Mode == EPlayerMode.Flak;
		AreaActionPreview? aimPort = null;
		AreaActionPreview? aimStarboard = null;
		if (aiming)
		{
			foreach (var preview in frame.AreaActions.Aim)
			{
				if (preview.Action is not FlakAction flak)
					continue;
				if (flak.MountedOn == ESpatialOrientation.Port)
					aimPort = preview;
				else if (flak.MountedOn == ESpatialOrientation.Starboard)
					aimStarboard = preview;
			}
		}

		var queued = frame.ShowWeaponPreviews
			? frame.AreaActions.Queued.LastOrDefault(preview => preview.Action is FlakAction)
			: null;
		_aimPort.Apply(aimPort?.Volume);
		_aimStarboard.Apply(aimStarboard?.Volume);
		_queued.Apply(queued?.Volume);
		Visible = aimPort is not null || aimStarboard is not null || queued is not null;

		var effectiveMount = frame.StagedMountedOn ?? frame.FlakHoverMountedOn;
		if (aimPort is not null)
		{
			WeaponPreviewMaterials.ApplyWireframe(
				_portMaterial,
				PortTint,
				Strength(effectiveMount == ESpatialOrientation.Port));
		}
		if (aimStarboard is not null)
		{
			WeaponPreviewMaterials.ApplyWireframe(
				_starboardMaterial,
				StarboardTint,
				Strength(effectiveMount == ESpatialOrientation.Starboard));
		}
	}

	private static float Strength(bool hovered) =>
		hovered ? HoverStrength : AimStrength;
}
