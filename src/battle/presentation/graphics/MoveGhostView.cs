using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveGhostView : Node3D
{
	private static readonly IReadOnlySet<Coord> NoHeadings = new HashSet<Coord>();
	private static readonly Color PassiveColor = new(0.58f, 0.6f, 0.62f);

	private UnitView? _view;
	private EType? _type;
	private bool? _selected;
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

	public void Apply(
		UnitDisplayState? state,
		IReadOnlySet<Coord> reachableHeadings,
		Color color,
		bool selected)
	{
		if (state is null)
		{
			Visible = false;
			_orientationOverlay.Apply(null, NoHeadings, null);
			return;
		}

		if (_view is null || _type != state.Type || _selected != selected)
		{
			_view?.QueueFree();
			_view = new UnitView { Name = "GhostUnit" };
			_view.Bind(state.ToState(), selected ? color : PassiveColor);
			AddChild(_view);
			_type = state.Type;
			_selected = selected;
		}

		_view.Sync(state.ToState());
		_view.SetGhost(selected);
		_orientationOverlay.Apply(
			selected ? state.Position : null,
			selected ? reachableHeadings : NoHeadings,
			selected ? state.Fore : null);
		Visible = true;
	}

	public void SetHoldProgress(Coord? heading, float progress) =>
		_orientationOverlay.SetHoldProgress(heading, progress);
}
