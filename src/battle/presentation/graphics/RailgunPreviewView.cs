using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class RailgunPreviewView : Node3D
{
	private const float AimStrength = 0.85f;
	private const float HoverStrength = 1.35f;

	private static readonly Color Tint = new(0.55f, 0.82f, 1f, 0.42f);
	private static readonly IReadOnlySet<Coord> NoCells = new HashSet<Coord>();

	private WeaponVolumeMeshSlot _aim = null!;
	private WeaponVolumeMeshSlot _queued = null!;
	private ShaderMaterial _aimMaterial = null!;
	private IReadOnlySet<Coord> _aimCells = NoCells;

	public void Build()
	{
		_aimMaterial = WeaponPreviewMaterials.CreateDotted(Tint);
		var queuedMaterial = WeaponPreviewMaterials.CreateDotted(WeaponPreviewMaterials.CementedTint);
		WeaponPreviewMaterials.ApplyCemented(queuedMaterial);
		_aim = new WeaponVolumeMeshSlot("RailgunAim", _aimMaterial);
		_queued = new WeaponVolumeMeshSlot("RailgunQueued", queuedMaterial);
		AddChild(_aim.Instance);
		AddChild(_queued.Instance);
		Visible = false;
	}

	public bool PickHovered(Camera3D camera, Vector2 screenPos)
	{
		if (!_aim.Instance.Visible)
			return false;

		return GridPick.PickFromSet(camera, screenPos, _aimCells) is not null;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		var aiming = frame.ShowWeaponPreviews && frame.Mode == EPlayerMode.Railgun;
		var aim = aiming
			? frame.AreaActions.Aim.FirstOrDefault(preview => preview.Action is RailgunAction)
			: null;
		var queued = frame.ShowWeaponPreviews
			? frame.AreaActions.Queued.LastOrDefault(preview => preview.Action is RailgunAction)
			: null;

		_aim.Apply(aim?.Volume);
		_queued.Apply(queued?.Volume);
		_aimCells = aim?.Volume.Cells ?? NoCells;
		Visible = aim is not null || queued is not null;

		if (aim is not null)
		{
			WeaponPreviewMaterials.ApplyAim(
				_aimMaterial,
				Tint,
				frame.RailgunHovered ? HoverStrength : AimStrength);
		}
	}
}
