using Godot;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveOrientationOverlay : Node2D
{
	private Camera3D _camera = null!;
	private Coord? _destination;
	private IReadOnlySet<Coord> _reachableHeadings = new HashSet<Coord>();
	private Coord? _selectedHeading;
	private Coord? _heldHeading;
	private Vector2? _shellCenter;
	private float _holdProgress;
	private readonly Dictionary<Coord, ArrowView> _arrows = [];

	public void Configure(Camera3D camera)
	{
		_camera = camera;
		foreach (var heading in MovePose.Headings)
		{
			var arrow = new ArrowView();
			arrow.Configure(HeadingDirectionColors.For(heading));
			AddChild(arrow);
			_arrows[heading] = arrow;
		}
	}

	public void Apply(Coord? destination, IReadOnlySet<Coord> reachableHeadings, Coord? selectedHeading)
	{
		_destination = destination;
		_reachableHeadings = reachableHeadings;
		_selectedHeading = selectedHeading;
		Visible = destination is not null;
		UpdateArrows();
	}

	public void SetHoldProgress(Coord? heading, float progress)
	{
		_heldHeading = heading;
		_holdProgress = System.Math.Clamp(progress, 0f, 1f);
		UpdateArrows();
	}

	public override void _Process(double delta)
	{
		if (Visible)
			UpdateArrows();
	}

	private void UpdateArrows()
	{
		foreach (var arrow in _arrows.Values)
			arrow.Visible = false;

		if (_destination is not { } destination || !IsInstanceValid(_camera))
		{
			_shellCenter = null;
			QueueRedraw();
			return;
		}

		var centerWorld = WorldMapping.ToWorld(destination);
		_shellCenter = _camera.IsPositionBehind(centerWorld)
			? null
			: _camera.UnprojectPosition(centerWorld);
		QueueRedraw();

		foreach (var handle in MovementSelection.ProjectHeadingHandles(
			_camera,
			destination,
			_reachableHeadings))
		{
			var arrow = _arrows[handle.Heading];
			arrow.Position = handle.Position;
			arrow.Visible = true;
			arrow.Apply(
				handle.Kind,
				handle.Rotation,
				selected: handle.Heading == _selectedHeading,
				progress: handle.Heading == _heldHeading ? _holdProgress : 0f);
		}
	}

	public override void _Draw()
	{
		if (_shellCenter is { } center)
			ProjectedDirectionHandleRenderer.DrawShell(this, center);
	}

	private sealed partial class ArrowView : Node2D
	{
		private MovementSelection.HeadingHandleKind _kind;
		private float _rotation;
		private bool _selected;
		private float _progress;
		private Color _color;

		public void Configure(Color color) => _color = color;

		public void Apply(
			MovementSelection.HeadingHandleKind kind,
			float rotation,
			bool selected,
			float progress)
		{
			_kind = kind;
			_rotation = rotation;
			_selected = selected;
			_progress = progress;
			QueueRedraw();
		}

		public override void _Draw()
			=> ProjectedDirectionHandleRenderer.Draw(
				this,
				Vector2.Zero,
				_rotation,
				_kind,
				_color,
				enabled: true,
				emphasized: _selected,
				_progress);

	}
}
