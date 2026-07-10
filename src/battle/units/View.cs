using Godot;
using GrimSpace.Battle.Grid;

namespace GrimSpace.Battle.Units;

public partial class View : Node3D
{
	private State? _state;

	public void Bind(State state, Color color)
	{
		_state = state;
		Name = state.Id;
		SyncPosition();

		var mesh = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(1.2f, 0.8f, 1.8f) },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color,
				Roughness = 0.6f,
			},
		};
		AddChild(mesh);
	}

	public void SyncPosition()
	{
		if (_state is null)
			return;

		Position = GrimSpace.Battle.Grid.View.ToWorld(_state.Position);
	}
}
