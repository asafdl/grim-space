using Godot;
using GrimSpace.Battle.Presentation.Graphics;

public partial class SparkExplosionPreview : Node3D
{
	public override void _Ready()
	{
		var timer = new Godot.Timer
		{
			WaitTime = 1.2,
			Autostart = true,
		};
		AddChild(timer);
		timer.Timeout += PlayExplosion;
		PlayExplosion();
	}

	private void PlayExplosion() =>
		OneShotParticles.PlayExplosionModel(this, GlobalPosition, 0.65f);
}
