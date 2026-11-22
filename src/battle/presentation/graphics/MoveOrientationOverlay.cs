using Godot;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveOrientationOverlay : Node2D
{
	private const float RollIconSize = 32f;
	private const float BuzzStepSeconds = 0.035f;
	private const float BuzzShakePixels = 5f;
	private Camera3D _camera = null!;
	private TextureRect _rollIcon = null!;
	private Coord? _destination;
	private IReadOnlySet<Coord> _reachableHeadings = new HashSet<Coord>();
	private Coord? _selectedHeading;
	private Coord? _heldHeading;
	private Vector2? _shellCenter;
	private float _holdProgress;
	private Vector2 _buzzOffset;
	private Tween? _buzzTween;
	private readonly RandomNumberGenerator _buzzRng = new();
	private readonly Dictionary<Coord, ArrowView> _arrows = [];

	public Control RollButton => _rollIcon;

	public void Configure(Camera3D camera)
	{
		_camera = camera;
		_rollIcon = new TextureRect
		{
			Texture = GD.Load<Texture2D>("res://assets/ui/battle/movement_roll.png"),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Size = Vector2.One * RollIconSize,
			MouseFilter = Control.MouseFilterEnum.Stop,
			Visible = false,
			ZIndex = 1,
		};
		AddChild(_rollIcon);
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
		if (destination is null)
			ClearBuzz();
		UpdateArrows();
	}

	public void SetHoldProgress(Coord? heading, float progress)
	{
		_heldHeading = heading;
		_holdProgress = System.Math.Clamp(progress, 0f, 1f);
		UpdateArrows();
	}

	public void NudgeAttention()
	{
		if (!Visible || _destination is null)
			return;

		_buzzTween?.Kill();
		_buzzTween = CreateTween();
		float[] amplitudes = [1f, 0.85f, 1f, 0.7f, 0.55f, 0.35f, 0f];
		foreach (var amplitude in amplitudes)
		{
			var strength = amplitude;
			_buzzTween.TweenCallback(Callable.From(() => SetBuzzOffset(strength)));
			_buzzTween.TweenInterval(BuzzStepSeconds);
		}
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
			_rollIcon.Visible = false;
			_shellCenter = null;
			QueueRedraw();
			return;
		}

		var centerWorld = WorldMapping.ToWorld(destination);
		_shellCenter = _camera.IsPositionBehind(centerWorld)
			? null
			: _camera.UnprojectPosition(centerWorld);
		_rollIcon.Visible = _shellCenter is not null;
		QueueRedraw();

		var handles = MovementSelection.ProjectHeadingHandles(
			_camera,
			destination,
			_reachableHeadings);
		foreach (var handle in handles)
		{
			var arrow = _arrows[handle.Heading];
			arrow.Position = handle.Position + _buzzOffset;
			arrow.Visible = true;
			arrow.Apply(
				handle.Kind,
				handle.Rotation,
				selected: handle.Heading == _selectedHeading,
				progress: handle.Heading == _heldHeading ? _holdProgress : 0f);
		}

		if (_shellCenter is { } center)
		{
			var cellWidth = center.DistanceTo(_camera.UnprojectPosition(
				centerWorld + _camera.GlobalBasis.X * WorldMapping.CellSize));
			var offset = Mathf.Max(48f, cellWidth * 0.55f);
			_rollIcon.Position = center + new Vector2(offset, -offset) - _rollIcon.Size * 0.5f;
		}
	}

	public override void _Draw()
	{
		if (_shellCenter is { } center)
			ProjectedDirectionHandleRenderer.DrawShell(this, center);
	}

	private void SetBuzzOffset(float strength)
	{
		if (strength <= 0f)
		{
			_buzzOffset = Vector2.Zero;
			UpdateArrows();
			return;
		}

		var angle = _buzzRng.Randf() * Mathf.Tau;
		_buzzOffset = Vector2.FromAngle(angle) * BuzzShakePixels * strength;
		UpdateArrows();
	}

	private void ClearBuzz()
	{
		_buzzTween?.Kill();
		_buzzTween = null;
		_buzzOffset = Vector2.Zero;
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
