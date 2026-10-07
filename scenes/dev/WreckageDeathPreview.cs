using Godot;
using GrimSpace.Battle.Presentation.Graphics;

public partial class WreckageDeathPreview : Node3D
{
	private const double IntactSeconds = 1.6;
	private const double AftermathSeconds = 8.5;
	private const float EffectScale = 1.1f;
	private const int DebrisCount = 10;

	private Godot.Timer _timer = null!;
	private Label _status = null!;
	private Node3D? _ship;

	public override void _Ready()
	{
		GetNode<Camera3D>("Camera3D").LookAt(Vector3.Zero, Vector3.Up);
		_status = GetNode<Label>("CanvasLayer/StatusLabel");
		_timer = new Godot.Timer { OneShot = true };
		_timer.Timeout += AdvancePreview;
		AddChild(_timer);

		SpawnShip();
	}

	public override void _Process(double delta)
	{
		if (_ship is not null)
			_ship.RotateY((float)delta * 0.2f);
	}

	private void AdvancePreview()
	{
		if (_ship is null)
			SpawnShip();
		else
			DestroyShip();
	}

	private void SpawnShip()
	{
		_ship = new Node3D { Name = "PreviewShip" };
		_ship.AddChild(ShipMesh.CreateFighterHull());
		AddChild(_ship);
		_status.Text = $"Ship destruction preview\nExplosion + {DebrisCount} drifting fragments";
		_timer.Start(IntactSeconds);
	}

	private void DestroyShip()
	{
		var worldPosition = _ship!.GlobalPosition;
		_ship.QueueFree();
		_ship = null;
		OneShotParticles.PlayShipDestruction(this, worldPosition, EffectScale, DebrisCount);
		_status.Text = "Non-interactive wreckage fades after 8 seconds";
		_timer.Start(AftermathSeconds);
	}
}
