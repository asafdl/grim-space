using Godot;
using GrimSpace.Battle;

namespace GrimSpace.Battle;

public partial class UnitView : Node3D
{
	private UnitState? _state;

	public void Bind(UnitState state)
	{
		_state = state;
		Name = state.Team == Team.Player ? "Player" : "Enemy";
		Position = GridView.GridToWorld(state.Position);

		var mesh = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(1.2f, 0.8f, 1.8f) },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = state.Team == Team.Player
					? new Color(0.2f, 0.55f, 0.95f)
					: new Color(0.9f, 0.25f, 0.2f),
				Roughness = 0.6f,
			},
		};
		AddChild(mesh);
	}

	public void SyncPosition()
	{
		if (_state is null)
			return;

		Position = GridView.GridToWorld(_state.Position);
	}
}
