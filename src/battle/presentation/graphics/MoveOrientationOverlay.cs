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
			var style = StyleFor(heading);
			arrow.Configure(style.Label, style.Color);
			AddChild(arrow);
			_arrows[heading] = arrow;
		}
	}

	private static DirectionStyle StyleFor(Coord heading) =>
		heading switch
		{
			{ X: 1, Y: 0, Z: 0 } => new("+X", new Color(1f, 0.34f, 0.28f)),
			{ X: -1, Y: 0, Z: 0 } => new("-X", new Color(1f, 0.56f, 0.18f)),
			{ X: 0, Y: 1, Z: 0 } => new("+Y", new Color(0.28f, 1f, 0.38f)),
			{ X: 0, Y: -1, Z: 0 } => new("-Y", new Color(0.78f, 0.94f, 0.2f)),
			{ X: 0, Y: 0, Z: 1 } => new("+Z", new Color(0.2f, 0.72f, 1f)),
			{ X: 0, Y: 0, Z: -1 } => new("-Z", new Color(0.58f, 0.38f, 1f)),
			_ => throw new ArgumentOutOfRangeException(nameof(heading), heading, null),
		};

	private readonly record struct DirectionStyle(string Label, Color Color);

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
			arrow.Rotation = handle.Kind == MovementSelection.HeadingHandleKind.Arrow
				? handle.Rotation
				: 0f;
			arrow.Visible = true;
			arrow.Apply(
				handle.Kind,
				selected: handle.Heading == _selectedHeading,
				progress: handle.Heading == _heldHeading ? _holdProgress : 0f);
		}
	}

	private sealed partial class ArrowView : Node2D
	{
		private static readonly Vector2[] Shape =
		[
			new(-30f, -8f),
			new(8f, -8f),
			new(8f, -18f),
			new(34f, 0f),
			new(8f, 18f),
			new(8f, 8f),
			new(-30f, 8f),
		];
		private static readonly Vector2[] Outline = [.. Shape, Shape[0]];

		private MovementSelection.HeadingHandleKind _kind;
		private bool _selected;
		private float _progress;
		private Color _bodyColor;
		private Color _fillColor;
		private Label _label = null!;

		public void Configure(string label, Color color)
		{
			_bodyColor = new Color(color.R, color.G, color.B, 0.72f);
			var fill = color.Lerp(Colors.White, 0.35f);
			_fillColor = new Color(fill.R, fill.G, fill.B, 0.96f);
			_label = new Label
			{
				Text = label,
				Position = new Vector2(-18f, 20f),
				Size = new Vector2(36f, 22f),
				PivotOffset = new Vector2(18f, 11f),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			_label.AddThemeFontSizeOverride("font_size", 12);
			_label.AddThemeColorOverride("font_color", Colors.White);
			_label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
			_label.AddThemeConstantOverride("outline_size", 4);
			AddChild(_label);
		}

		public void Apply(MovementSelection.HeadingHandleKind kind, bool selected, float progress)
		{
			_kind = kind;
			_selected = selected;
			_progress = progress;
			_label.Rotation = -Rotation;
			QueueRedraw();
		}

		public override void _Draw()
		{
			var outlineColor = _selected
				? new Color(0.92f, 0.95f, 1f, 0.95f)
				: new Color(0.72f, 0.75f, 0.8f, 0.72f);

			if (_kind == MovementSelection.HeadingHandleKind.Arrow)
			{
				DrawColoredPolygon(Shape.Select(point => point + new Vector2(3f, 3f)).ToArray(), new Color(0f, 0f, 0f, 0.55f));
				DrawColoredPolygon(Shape, _bodyColor);
				if (_progress > 0f)
					DrawColoredPolygon(ClipAt(Shape, -30f + 64f * _progress), _fillColor);
				DrawPolyline(Outline, outlineColor, 2f, antialiased: true);
				return;
			}

			DrawCircle(new Vector2(3f, 3f), 18f, new Color(0f, 0f, 0f, 0.55f));
			DrawCircle(Vector2.Zero, 18f, _bodyColor);
			DrawArc(Vector2.Zero, 18f, 0f, Mathf.Tau, 32, outlineColor, 2f, antialiased: true);
			if (_progress > 0f)
			{
				DrawArc(
					Vector2.Zero,
					15f,
					-Mathf.Pi / 2f,
					-Mathf.Pi / 2f + Mathf.Tau * _progress,
					32,
					_fillColor,
					5f,
					antialiased: true);
			}

			if (_kind == MovementSelection.HeadingHandleKind.TowardCamera)
				DrawCircle(Vector2.Zero, 5f, outlineColor);
			else
			{
				DrawLine(new Vector2(-7f, -7f), new Vector2(7f, 7f), outlineColor, 3f, antialiased: true);
				DrawLine(new Vector2(-7f, 7f), new Vector2(7f, -7f), outlineColor, 3f, antialiased: true);
			}
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
