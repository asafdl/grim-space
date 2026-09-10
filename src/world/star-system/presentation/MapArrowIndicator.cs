using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class MapArrowIndicator : Node3D
{
	private const float HoverHeight = 1.35f;
	private const float BobAmplitude = 0.15f;
	private const float VisualScale = 0.45f;
	private static readonly Color Color = new(0.35f, 0.85f, 0.95f, 0.95f);

	public override void _Ready()
	{
		var arrow = CreateArrow();
		arrow.Position = new Vector3(0f, HoverHeight, 0f);
		arrow.Rotation = new Vector3(Mathf.Pi, 0f, 0f);
		arrow.Scale = Vector3.One * VisualScale;
		AddChild(arrow);

		var bob = arrow.CreateTween().SetLoops();
		bob.TweenProperty(arrow, "position:y", HoverHeight - BobAmplitude, 0.55)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		bob.TweenProperty(arrow, "position:y", HoverHeight + BobAmplitude * 0.25f, 0.55)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
	}

	private static Node3D CreateArrow()
	{
		var arrow = new Node3D { Name = "Arrow" };
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Color,
			EmissionEnabled = true,
			Emission = Color with { A = 1f },
			EmissionEnergyMultiplier = 2.4f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
		};

		arrow.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh
			{
				TopRadius = 0.12f,
				BottomRadius = 0.12f,
				Height = 0.9f,
			},
			Position = new Vector3(0f, 0.45f, 0f),
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
		arrow.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh
			{
				TopRadius = 0f,
				BottomRadius = 0.42f,
				Height = 0.62f,
			},
			Position = new Vector3(0f, 1.2f, 0f),
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
		return arrow;
	}
}
