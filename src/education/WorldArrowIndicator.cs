using Godot;
using GrimSpace.Components;

namespace GrimSpace.Education;

public sealed partial class WorldArrowIndicator : Node3D
{
	private const float ReferenceDistance = 22f;
	private const float VisualScale = 1.71f;
	private const float TurnSpeed = 0.35f;
	private const string ReticleTexturePath =
		"res://assets/ui/components/runtime/reticle_hollow_neutral.png";
	private Sprite3D _reticle = null!;
	private float _turnAngle;

	public override void _Ready()
	{
		_reticle = CreateReticle();
		AddChild(_reticle);
		UpdatePose();
	}

	public override void _Process(double delta)
	{
		_turnAngle = Mathf.Wrap(
			_turnAngle + (float)delta * TurnSpeed,
			-Mathf.Pi,
			Mathf.Pi);
		UpdatePose();
	}

	private static Sprite3D CreateReticle()
	{
		var reticle = new Sprite3D
		{
			Name = "Reticle",
			Texture = GD.Load<Texture2D>(ReticleTexturePath)
				?? throw new InvalidOperationException(
					$"Could not load reticle texture '{ReticleTexturePath}'."),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			Shaded = false,
			PixelSize = 0.0027f,
			Modulate = HudPalettes.WithAlpha(HudPalettes.BlueCyan.Secondary, 0.8f),
		};
		return reticle;
	}

	private void UpdatePose()
	{
		var camera = GetViewport().GetCamera3D();
		if (camera is null)
			return;

		var target = GlobalPosition;
		var cameraDistance = Mathf.Max(camera.GlobalPosition.DistanceTo(target), 1f);
		var distanceScale = cameraDistance / ReferenceDistance;
		var reticleScale = VisualScale * distanceScale;

		_reticle.GlobalPosition = target;
		_reticle.Scale = Vector3.One * reticleScale;
		_reticle.Rotation = new Vector3(0f, 0f, _turnAngle);
	}
}
