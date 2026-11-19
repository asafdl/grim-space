using Godot;

namespace GrimSpace.Presentation;

public sealed partial class ClickFeedback : Node2D
{
	private const double DurationSeconds = 0.15;
	private readonly List<(Vector2 Position, double Age)> _pulses = [];

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetProcess(false);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
			return;

		var position = GetGlobalTransformWithCanvas().AffineInverse() * click.Position;
		_pulses.Add((position, 0));
		SetProcess(true);
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		for (var i = _pulses.Count - 1; i >= 0; i--)
		{
			var pulse = _pulses[i];
			var age = pulse.Age + delta;
			if (age >= DurationSeconds)
				_pulses.RemoveAt(i);
			else
				_pulses[i] = (pulse.Position, age);
		}

		QueueRedraw();
		SetProcess(_pulses.Count > 0);
	}

	public override void _Draw()
	{
		foreach (var pulse in _pulses)
		{
			var progress = (float)(pulse.Age / DurationSeconds);
			var radius = Mathf.Lerp(5f, 18f, progress);
			var opacity = 1f - progress;
			DrawArc(pulse.Position, radius, 0f, Mathf.Tau, 48,
				new Color(0.08f, 0.02f, 0.03f, opacity * 0.6f), 3f, true);
			DrawArc(pulse.Position, radius, 0f, Mathf.Tau, 48,
				new Color(1f, 0.85f, 0.85f, opacity * 0.8f), 1.5f, true);
		}
	}
}
