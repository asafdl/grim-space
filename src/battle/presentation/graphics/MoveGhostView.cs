using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveGhostView : Node3D
{
	private static readonly IReadOnlySet<Coord> NoHeadings = new HashSet<Coord>();
	private static readonly Color PassiveColor = new(0.58f, 0.6f, 0.62f);
	private const float PathGhostScale = 0.5f;

	private UnitView? _view;
	private EType? _type;
	private bool? _selected;
	private readonly List<(EType Type, UnitView View)> _activePathViews = [];
	private readonly Dictionary<EType, Queue<UnitView>> _freePathViews = [];
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
		IReadOnlyList<MoveCheckpoint> checkpoints,
		UnitDisplayState pathTemplate,
		IReadOnlySet<Coord> reachableHeadings,
		Color color,
		bool selected)
	{
		ApplyPath(checkpoints, pathTemplate);
		if (state is null)
		{
			if (_view is not null)
				_view.Visible = false;
			_orientationOverlay.Apply(null, NoHeadings, null);
			Visible = _activePathViews.Count > 0;
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

	private void ApplyPath(IReadOnlyList<MoveCheckpoint> checkpoints, UnitDisplayState template)
	{
		ReleasePathViews();
		for (var i = 0; i < checkpoints.Count - 1; i++)
		{
			var checkpoint = checkpoints[i];
			var state = template with
			{
				Position = checkpoint.Position,
				Fore = checkpoint.Basis.Forward,
				Dorsal = checkpoint.Basis.Up,
			};
			var view = AcquirePathView(state);
			view.Sync(state.ToState());
			view.Scale = Vector3.One * PathGhostScale;
			view.Visible = true;
			_activePathViews.Add((state.Type, view));
		}
	}

	private UnitView AcquirePathView(UnitDisplayState state)
	{
		if (_freePathViews.TryGetValue(state.Type, out var free)
			&& free.TryDequeue(out var existing))
			return existing;

		var view = new UnitView
		{
			Name = "PathGhost",
		};
		view.Bind(state.ToState(), PassiveColor);
		view.SetGhost(selected: false);
		AddChild(view);
		return view;
	}

	private void ReleasePathViews()
	{
		foreach (var (type, view) in _activePathViews)
		{
			view.Visible = false;
			if (!_freePathViews.TryGetValue(type, out var free))
			{
				free = [];
				_freePathViews.Add(type, free);
			}
			free.Enqueue(view);
		}
		_activePathViews.Clear();
	}
}
