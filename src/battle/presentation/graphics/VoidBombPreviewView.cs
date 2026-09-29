using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class VoidBombPreviewView : Node3D
{
	private static readonly Color[] TurnTints =
	[
		new(0.15f, 0.90f, 1.00f, 0.16f),
		new(0.25f, 0.55f, 1.00f, 0.10f),
		new(0.65f, 0.30f, 1.00f, 0.05f),
	];

	private TurnVolumeWireframe? _aimTravel;

	internal void Build(CellVolumeMeshStore meshes)
	{
		_aimTravel = new TurnVolumeWireframe("TorpedoAimTurn", TurnTints, meshes);
		foreach (var instance in _aimTravel.Instances)
			AddChild(instance);

		Visible = false;
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		var aiming = frame.ShowWeaponPreviews && frame.Mode == EPlayerMode.VoidBomb;
		_aimTravel?.Apply(
			aiming ? frame.VoidBombPreviews.Aim : null,
			frame.SimulationTick);
		Visible = aiming && frame.VoidBombPreviews.Aim is not null;
	}
}
