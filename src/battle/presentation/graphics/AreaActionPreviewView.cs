using Godot;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class AreaActionPreviewView : Node3D
{
	private const float HoverStrength = 1.35f;

	private readonly List<QueuedSlot> _queuedSlots = [];
	private CellVolumeMeshStore _meshes = null!;
	private CellVolumeWireframeSlot _aim = null!;
	private StandardMaterial3D _aimMaterial = null!;

	internal void Build(CellVolumeMeshStore meshes)
	{
		_meshes = meshes;
		_aimMaterial = WeaponPreviewMaterials.CreateWireframe(Colors.White);
		_aim = new CellVolumeWireframeSlot("AreaActionAim", _aimMaterial, meshes);
		AddChild(_aim.Instance);
		Visible = false;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		var hovered = frame.HoveredAbilityChoice;
		var aim = hovered is not null
			? frame.AreaActions.Aim.FirstOrDefault(
				preview => ReferenceEquals(preview.Action, hovered.Action))
			: null;
		_aim.Apply(aim?.Volume, frame.SimulationTick);
		if (aim is not null)
		{
			WeaponPreviewMaterials.ApplyWireframe(
				_aimMaterial,
				hovered!.Targeting.Tint,
				HoverStrength);
		}

		var queued = frame.ShowWeaponPreviews
			? frame.AreaActions.Queued
			: [];
		EnsureQueuedSlots(queued.Count);
		for (var i = 0; i < _queuedSlots.Count; i++)
		{
			_queuedSlots[i].Apply(
				i < queued.Count ? queued[i] : null,
				frame.SimulationTick);
		}

		Visible = aim is not null || queued.Count > 0;
	}

	private void EnsureQueuedSlots(int count)
	{
		while (_queuedSlots.Count < count)
		{
			var slot = new QueuedSlot(_queuedSlots.Count, _meshes);
			_queuedSlots.Add(slot);
			AddChild(slot.Wireframe.Instance);
		}
	}

	private sealed class QueuedSlot
	{
		public QueuedSlot(int index, CellVolumeMeshStore meshes)
		{
			var material = WeaponPreviewMaterials.CreateWireframe(
				WeaponPreviewMaterials.CementedTint);
			Wireframe = new CellVolumeWireframeSlot(
				$"AreaActionQueued{index}",
				material,
				meshes);
		}

		public CellVolumeWireframeSlot Wireframe { get; }

		public void Apply(AreaActionPreview? preview, int tick) =>
			Wireframe.Apply(preview?.Volume, tick);
	}
}
