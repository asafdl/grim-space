using Godot;

namespace GrimSpace.Education;

public sealed partial class WorldArrowIndicator : Node3D
{
	private const float ReferenceDistance = 22f;
	private const float OffsetAtReferenceDistance = 1.2f;
	private const float VisualScale = 0.45f;
	private static readonly Color Color = new(0.35f, 0.85f, 0.95f, 0.95f);
	private Node3D _arrow = null!;

	public override void _Ready()
	{
		_arrow = CreateArrow();
		AddChild(_arrow);
		UpdatePose();
	}

	public override void _Process(double _) => UpdatePose();

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

	private void UpdatePose()
	{
		var camera = GetViewport().GetCamera3D();
		if (camera is null)
			return;

		var target = GlobalPosition;
		var cameraDistance = Mathf.Max(camera.GlobalPosition.DistanceTo(target), 1f);
		var distanceScale = cameraDistance / ReferenceDistance;
		var screenOffset = (-camera.GlobalBasis.X + camera.GlobalBasis.Y * 0.8f).Normalized();
		var position = target + screenOffset * OffsetAtReferenceDistance * distanceScale;

		_arrow.GlobalPosition = position;
		_arrow.LookAt(target, camera.GlobalBasis.Y);
		_arrow.RotateObjectLocal(Vector3.Right, -Mathf.Pi / 2f);
		_arrow.Scale = Vector3.One * VisualScale * distanceScale;
	}
}
