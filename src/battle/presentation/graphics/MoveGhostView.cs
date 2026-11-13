using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveGhostView : Node3D
{
	public bool ShowAxisGuides { get; init; } = true;

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
	private MultiMeshInstance3D _axisGuides = null!;

	public Node3D? GhostShip => _view is { Visible: true } ship ? ship : null;
	public Control RollButton { get; private set; } = null!;

	public void Configure(Camera3D camera) => _camera = camera;

	public override void _Ready()
	{
		var layer = new CanvasLayer { Layer = 20 };
		_orientationOverlay = new MoveOrientationOverlay { Name = "OrientationOverlay" };
		_orientationOverlay.Configure(_camera);
		RollButton = _orientationOverlay.RollButton;
		layer.AddChild(_orientationOverlay);
		AddChild(layer);
		_axisGuides = CreateAxisGuides();
		AddChild(_axisGuides);
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
		_axisGuides.Visible = ShowAxisGuides && state is not null;
		if (state is not null)
			_axisGuides.Position = WorldMapping.ToWorld(state.Position);
		if (state is null)
		{
			if (_view is not null)
				_view.HideVisual();
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

		_view.Present(
			state.ToState(),
			selected ? UnitVisualState.SelectedMoveGhost : UnitVisualState.MoveGhost);
		_orientationOverlay.Apply(
			selected ? state.Position : null,
			selected ? reachableHeadings : NoHeadings,
			selected ? state.Fore : null);
		Visible = true;
	}

	public void SetHoldProgress(Coord? heading, float progress) =>
		_orientationOverlay.SetHoldProgress(heading, progress);

	private static MultiMeshInstance3D CreateAxisGuides()
	{
		const int dotsPerRay = 9;
		var axes = new[]
		{
			(Vector3.Right, new Color(1f, 0.35f, 0.3f)),
			(Vector3.Up, new Color(0.35f, 1f, 0.45f)),
			(Vector3.Back, new Color(0.35f, 0.65f, 1f)),
		};
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
		};
		var dots = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = new SphereMesh
			{
				Radius = 0.045f,
				Height = 0.09f,
				RadialSegments = 8,
				Rings = 4,
				Material = material,
			},
			InstanceCount = axes.Length * 2 * dotsPerRay,
		};
		var index = 0;
		foreach (var (axis, color) in axes)
		{
			foreach (var sign in new[] { -1, 1 })
			{
				for (var dot = 1; dot <= dotsPerRay; dot++)
				{
					var offset = axis * sign * dot * (WorldMapping.CellSize / 3f);
					dots.SetInstanceTransform(index, new Transform3D(Basis.Identity, offset));
					dots.SetInstanceColor(index++, color);
				}
			}
		}
		var guides = new MultiMeshInstance3D
		{
			Name = "DottedAxisGuides",
			Multimesh = dots,
			Visible = false,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(guides);
		return guides;
	}

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
			view.Present(state.ToState(), UnitVisualState.Ghost);
			view.Scale = Vector3.One * PathGhostScale;
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
		AddChild(view);
		return view;
	}

	private void ReleasePathViews()
	{
		foreach (var (type, view) in _activePathViews)
		{
			view.HideVisual();
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
