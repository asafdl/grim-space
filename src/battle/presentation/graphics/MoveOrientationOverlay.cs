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
		private static readonly Texture2D HandDrawnArrow = GD.Load<Texture2D>(
			"res://assets/ui/battle/heading_arrow_handdrawn.svg");
		private static readonly Rect2 HandDrawnRect = new(-36f, -22.5f, 72f, 45f);

		private MovementSelection.HeadingHandleKind _kind;
		private bool _selected;
		private float _progress;
		private Color _bodyColor;
		private Color _fillColor;

		public void Configure(Color color)
		{
			_bodyColor = new Color(color.R, color.G, color.B, 0.9f);
			var fill = color.Lerp(Colors.White, 0.35f);
			_fillColor = new Color(fill.R, fill.G, fill.B, 0.96f);
		}

		public void Apply(MovementSelection.HeadingHandleKind kind, bool selected, float progress)
		{
			_kind = kind;
			_selected = selected;
			_progress = progress;
			QueueRedraw();
		}

		public override void _Draw()
		{
			var outlineColor = _selected
				? new Color(0.92f, 0.95f, 1f, 0.95f)
				: new Color(0.72f, 0.75f, 0.8f, 0.72f);

			if (_kind == MovementSelection.HeadingHandleKind.Arrow)
			{
				if (_selected)
				{
					DrawTextureRect(
						HandDrawnArrow,
						HandDrawnRect.Grow(3f),
						tile: false,
						new Color(0.95f, 0.97f, 1f, 0.6f));
				}

				DrawTextureRect(
					HandDrawnArrow,
					new Rect2(HandDrawnRect.Position + new Vector2(3f, 3f), HandDrawnRect.Size),
					tile: false,
					new Color(0f, 0f, 0f, 0.45f));
				DrawTextureRect(HandDrawnArrow, HandDrawnRect, tile: false, _bodyColor);

				if (_progress > 0f)
				{
					var end = new Vector2(Mathf.Lerp(-24f, 25f, _progress), 0f);
					DrawLine(new Vector2(-24f, 0f), end, new Color(0f, 0f, 0f, 0.7f), 7f, antialiased: true);
					DrawLine(new Vector2(-24f, 0f), end, _fillColor, 4f, antialiased: true);
				}
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

	}
}
