using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveGhostView : Node3D
{
	private UnitView? _view;
	private EType? _type;
	private Camera3D _camera = null!;
	private MoveOrientationOverlay _orientationOverlay = null!;

	public void Configure(Camera3D camera) => _camera = camera;

	public override void _Ready()
	{
		var layer = new CanvasLayer { Layer = 20 };
		_orientationOverlay = new MoveOrientationOverlay { Name = "OrientationOverlay" };
		_orientationOverlay.Configure(_camera);
		layer.AddChild(_orientationOverlay);
		AddChild(layer);
	}

	public void Apply(UnitDisplayState? state, IReadOnlySet<Coord> reachableHeadings, Color color)
	{
		if (state is null)
		{
			Visible = false;
			_orientationOverlay.Apply(null, new HashSet<Coord>(), null);
			return;
		}

		if (_view is null || _type != state.Type)
		{
			_view?.QueueFree();
			_view = new UnitView { Name = "GhostUnit" };
			_view.Bind(state.ToState(), color);
			AddChild(_view);
			_type = state.Type;
		}

		_view.Sync(state.ToState());
		_view.SetGhost(true);
		_orientationOverlay.Apply(state.Position, reachableHeadings, state.Fore);
		Visible = true;
	}

	public void SetHoldProgress(Coord? heading, float progress) =>
		_orientationOverlay.SetHoldProgress(heading, progress);
}
