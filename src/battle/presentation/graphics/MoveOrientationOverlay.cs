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
	private float _holdProgress;
	private readonly Dictionary<Coord, ArrowView> _arrows = [];

	public void Configure(Camera3D camera)
	{
		_camera = camera;
		foreach (var heading in MovePose.Headings)
		{
			var arrow = new ArrowView();
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
			return;

		foreach (var handle in MovementSelection.ProjectHeadingHandles(
			_camera,
			destination,
			_reachableHeadings))
		{
			var arrow = _arrows[handle.Heading];
			arrow.Position = handle.Position;
			arrow.Rotation = handle.Rotation;
			arrow.Visible = true;
			arrow.Apply(
				selected: handle.Heading == _selectedHeading,
				progress: handle.Heading == _heldHeading ? _holdProgress : 0f);
		}
	}

	private sealed partial class ArrowView : Node2D
	{
		private static readonly Vector2[] Shape =
		[
			new(-20f, -5f),
			new(6f, -5f),
			new(6f, -12f),
			new(22f, 0f),
			new(6f, 12f),
			new(6f, 5f),
			new(-20f, 5f),
		];

		private readonly Polygon2D _shadow;
		private readonly Polygon2D _body;
		private readonly Polygon2D _fill;
		private readonly Line2D _outline;

		public ArrowView()
		{
			_shadow = new Polygon2D
			{
				Polygon = Shape,
				Position = new Vector2(3f, 3f),
				Color = new Color(0f, 0f, 0f, 0.55f),
			};
			AddChild(_shadow);

			_body = new Polygon2D
			{
				Polygon = Shape,
				Color = new Color(0.08f, 0.11f, 0.16f, 0.94f),
			};
			AddChild(_body);

			_fill = new Polygon2D
			{
				Color = new Color(0.35f, 0.78f, 1f, 0.95f),
			};
			AddChild(_fill);

			_outline = new Line2D
			{
				Points = [.. Shape, Shape[0]],
				Width = 2f,
				Antialiased = true,
			};
			AddChild(_outline);
		}

		public void Apply(bool selected, float progress)
		{
			_outline.DefaultColor = selected
				? new Color(0.8f, 0.95f, 1f)
				: new Color(0.55f, 0.65f, 0.75f);
			_fill.Polygon = ClipAt(Shape, -20f + 42f * progress);
		}

		private static Vector2[] ClipAt(IReadOnlyList<Vector2> polygon, float maxX)
		{
			var output = new List<Vector2>();
			var previous = polygon[^1];
			var previousInside = previous.X <= maxX;

			foreach (var current in polygon)
			{
				var currentInside = current.X <= maxX;
				if (currentInside != previousInside)
				{
					var amount = (maxX - previous.X) / (current.X - previous.X);
					output.Add(previous.Lerp(current, amount));
				}
				if (currentInside)
					output.Add(current);

				previous = current;
				previousInside = currentInside;
			}

			return output.ToArray();
		}
	}
}
