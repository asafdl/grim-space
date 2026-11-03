using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Dev;

public partial class HighVelocityRodPreview : Node3D
{
	private Camera3D _camera = null!;
	private HighVelocityRodEffect _rod = null!;
	private Label _status = null!;
	private float _angle;
	private float _elevation;
	private Tween? _motion;
	private bool _moving = true;
	private bool _paused;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.012f, 0.018f, 0.025f),
				GlowEnabled = true,
				GlowIntensity = 0.6f,
				GlowHdrThreshold = 1.5f,
			},
		});
		_camera = new Camera3D { Current = true, Projection = Camera3D.ProjectionType.Orthogonal, Size = 6 };
		AddChild(_camera);
		_rod = new HighVelocityRodEffect { Position = Vector3.Right * 1.3f };
		AddChild(_rod);
		var layer = new CanvasLayer();
		AddChild(layer);
		_status = new Label { Position = new Vector2(24, 24) };
		_status.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(_status);
		var arguments = OS.GetCmdlineUserArgs();
		if (arguments.Contains("--oblique"))
		{
			_angle = 0.6f;
			_elevation = 0.35f;
		}
		if (arguments.Contains("--rotated"))
			_rod.RotateZ(Mathf.Pi / 2);
		UpdateCamera();
		if (arguments.Contains("--battle-shot"))
		{
			_rod.Visible = false;
			_camera.Size = 12;
			var bursts = new HazardBurstView { Position = new Vector3(-1, -1, -1) };
			AddChild(bursts);
			var vertical = arguments.Contains("--rotated");
			var origin = vertical ? new Coord(0, -2, 0) : new Coord(2, 0, 0);
			var destination = vertical ? new Coord(0, 2, 0) : new Coord(-2, 0, 0);
			if (arguments.Contains("--short"))
				destination = origin + (vertical ? Coord.Up : new Coord(-1, 0, 0));
			bursts.PlayRodShot(origin, destination, Coord.Forward);
			return;
		}
		StartMotion();
	}

	private void StartMotion()
	{
		_motion?.Kill();
		_paused = false;
		_rod.SetRodReveal(0);
		_rod.SetFlareGlow(0);
		_motion = CreateTween().SetLoops();
		_motion.TweenMethod(Callable.From<float>(_rod.SetFlareGlow), 0f, 1f, HighVelocityRodEffect.ChargeSeconds)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		_motion.TweenCallback(Callable.From(_rod.ShowPing));
		_motion.TweenMethod(Callable.From<float>(_rod.SetRodReveal), 0f, 1f, HighVelocityRodEffect.RevealSeconds);
		_motion.TweenMethod(Callable.From<float>(_rod.SetFlareGlow), 1f, 0f, HighVelocityRodEffect.FadeSeconds);
		_motion.TweenInterval(0.3);
		_motion.TweenCallback(Callable.From(() => _rod.SetRodReveal(0)));
	}

	public override void _ExitTree() => _motion?.Kill();

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		switch (key.Keycode)
		{
			case Key.Space:
				_moving = !_moving;
				if (_moving)
					StartMotion();
				else
				{
					_motion?.Kill();
					_rod.SetRodReveal(1);
					_rod.SetFlareGlow(1);
				}
				break;
			case Key.P:
				_paused = !_paused;
				if (_paused)
					_motion?.Pause();
				else
					_motion?.Play();
				break;
			case Key.Left: _angle -= 0.2f; break;
			case Key.Right: _angle += 0.2f; break;
			case Key.Up: _elevation = Mathf.Min(1.2f, _elevation + 0.15f); break;
			case Key.Down: _elevation = Mathf.Max(-1.2f, _elevation - 0.15f); break;
			case Key.R:
				_angle = 0;
				_elevation = 0;
				_rod.Rotation = Vector3.Zero;
				break;
			case Key.T: _rod.RotateZ(Mathf.Pi / 2); break;
			default: return;
		}
		UpdateCamera();
		GetViewport().SetInputAsHandled();
	}

	private void UpdateCamera()
	{
		_camera.Position = new Vector3(
			Mathf.Sin(_angle) * Mathf.Cos(_elevation),
			Mathf.Sin(_elevation),
			Mathf.Cos(_angle) * Mathf.Cos(_elevation)) * 8;
		_camera.LookAt(Vector3.Zero);
		_status.Text = "High Velocity Rod - two gray shades + orange glow\n"
			+ "1s glow buildup, 0.01s right-to-left rod reveal, 0.25s glow fade\n"
			+ "Space: animation/static | P: pause/resume | Arrows: orbit | T: rotate effect | R: reset view";
	}
}
