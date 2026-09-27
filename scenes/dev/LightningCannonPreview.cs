using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Dev;

public partial class LightningCannonPreview : Node3D
{
	private const float RepeatSeconds = 0.8f;
	private int _lineLength = 5;
	private int _pyramidRange = 2;
	private float _timeUntilFire;
	private float _zoom = 1f;
	private bool _frozen;
	private LightningCannonEffect? _effect;
	private Label _status = null!;

	public override void _Ready()
	{
		_status = GetNode<Label>("UI/Status");
		UpdateStatus();
		Fire();
	}

	public override void _Process(double delta)
	{
		if (_frozen)
			return;

		_timeUntilFire -= (float)delta;
		if (_timeUntilFire <= 0f)
			Fire();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } mouse)
			return;

		switch (mouse.ButtonIndex)
		{
			case MouseButton.WheelUp:
				_zoom = Mathf.Max(0.35f, _zoom * 0.85f);
				break;
			case MouseButton.WheelDown:
				_zoom = Mathf.Min(2.5f, _zoom / 0.85f);
				break;
			default:
				return;
		}

		UpdateStatus();
		GetViewport().SetInputAsHandled();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Up:
				_lineLength = System.Math.Min(_lineLength + 1, 12);
				break;
			case Key.Down:
				_lineLength = System.Math.Max(_lineLength - 1, 1);
				break;
			case Key.Right:
				_pyramidRange = System.Math.Min(_pyramidRange + 1, 5);
				break;
			case Key.Left:
				_pyramidRange = System.Math.Max(_pyramidRange - 1, 0);
				break;
			case Key.Space:
				break;
			case Key.P:
				_frozen = !_frozen;
				_effect?.QueueFree();
				break;
			default:
				return;
		}

		UpdateStatus();
		Fire();
		GetViewport().SetInputAsHandled();
	}

	private void Fire()
	{
		_effect = LightningCannonEffect.Fire(
			this, Vector3.Zero, Vector3.Right, Vector3.Up,
			WorldMapping.CellSize, _lineLength, _pyramidRange);
		if (_frozen)
			_effect.ProcessMode = ProcessModeEnum.Disabled;
		_timeUntilFire = RepeatSeconds;
	}

	private void UpdateStatus()
	{
		var extent = (_lineLength + _pyramidRange) * WorldMapping.CellSize;
		var camera = GetNode<Camera3D>("Camera3D");
		camera.Position = new Vector3(extent * 0.5f, extent * 0.35f * _zoom, extent * 0.7f * _zoom);
		camera.LookAt(new Vector3(extent * 0.5f, 0f, 0f));
		_status.Text =
			$"Lightning cannon preview  |  Line: {_lineLength} cells  |  Fork: {_pyramidRange} cells  |  {(_frozen ? "Frozen" : "Looping")}\n" +
			"Mouse wheel: zoom   P: freeze/resume   Up/Down: line   Left/Right: fork   Space: fire";
	}
}
