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
	private CellVolumeWireframeSlot _aim = null!;
	private CellVolumeWireframeSlot _queued = null!;
	private StandardMaterial3D _aimMaterial = null!;

	internal void Build(CellVolumeMeshStore meshes)
	{
		_aimMaterial = WeaponPreviewMaterials.CreateWireframe(Tint);
		var queuedMaterial = WeaponPreviewMaterials.CreateWireframe(
			WeaponPreviewMaterials.CementedTint);
		_aim = new CellVolumeWireframeSlot("RailgunAim", _aimMaterial, meshes);
		_queued = new CellVolumeWireframeSlot("RailgunQueued", queuedMaterial, meshes);
		AddChild(_aim.Instance);
		AddChild(_queued.Instance);
		Visible = false;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		var aiming = frame.ShowWeaponPreviews
			&& frame.Mode == EPlayerMode.Railgun
			&& frame.HoveredAbilityChoice?.Action is RailgunAction;
		var aim = aiming
			? frame.AreaActions.Aim.FirstOrDefault(preview => preview.Action is RailgunAction)
			: null;
		var queued = frame.ShowWeaponPreviews
			? frame.AreaActions.Queued.LastOrDefault(preview => preview.Action is RailgunAction)
			: null;

		_aim.Apply(aim?.Volume, frame.SimulationTick);
		_queued.Apply(queued?.Volume, frame.SimulationTick);
		Visible = aim is not null || queued is not null;

		if (aim is not null)
		{
			WeaponPreviewMaterials.ApplyWireframe(
				_aimMaterial,
				Tint,
				HoverStrength);
		}
	}
}
