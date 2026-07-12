using Godot;
using GrimSpace.Battle.Grid;
using GrimSpace.Domain.Units;

namespace GrimSpace.Battle.Units;

public partial class View : Node3D
{
	private State? _state;
	private Label3D? _momentumLabel;

	public void Bind(State state, Color color)
	{
		_state = state;
		Name = state.Id;
		SyncPosition();

		var hull = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateHull(),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color,
				Roughness = 0.45f,
				Metallic = 0.15f,
			},
		};
		AddChild(hull);

		var nose = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateNoseMarker(),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.35f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.5f),
				EmissionEnergyMultiplier = 0.6f,
				Roughness = 0.3f,
			},
		};
		AddChild(nose);

		_momentumLabel = new Label3D
		{
			Position = new Vector3(0f, 1.2f, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 48,
			OutlineSize = 8,
			Modulate = Colors.White,
		};
		AddChild(_momentumLabel);
		SyncStatus();
	}

	public void SyncFromState(State displayState)
	{
		Position = GrimSpace.Battle.Grid.View.ToWorld(displayState.Position);
		SyncOrientation(displayState);
		SyncStatus(displayState);
	}

	public void SyncPosition()
	{
		if (_state is null)
			return;

		SyncFromState(_state);
	}

	public void SyncStatus()
	{
		if (_state is null)
			return;

		SyncStatus(_state);
	}

	private void SyncStatus(State displayState)
	{
		if (_momentumLabel is null)
			return;

		var evasion = (int)(MomentumConfig.ForLevel(displayState.MomentumLevel).Evasion * 100);
		_momentumLabel.Text = $"HP{displayState.Hp} M{displayState.MomentumLevel} ({evasion}%)";
	}

	private void SyncOrientation(State displayState)
	{
		var forward = displayState.ForwardDirection;
		if (forward == GrimSpace.Domain.Grid.Coord.Forward)
			Rotation = Vector3.Zero;
		else if (forward == GrimSpace.Domain.Grid.Coord.Zero - GrimSpace.Domain.Grid.Coord.Forward)
			Rotation = new Vector3(0f, Mathf.Pi, 0f);
		else
			Rotation = Vector3.Zero;
	}
}
