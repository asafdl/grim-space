using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Combat-intro markers: downward arrows pointing at opponent cells.
/// </summary>
public partial class OpponentIntroMarkersView : Node3D
{
	private const float ArrowHoverHeight = 2.8f;
	private const float ArrowBobAmplitude = 0.28f;

	private readonly List<Node3D> _markers = new();

	public void Show(IReadOnlyCollection<Coord> cells)
	{
		Clear();
		foreach (var cell in cells)
			AddMarker(cell);
	}

	public void Clear()
	{
		foreach (var marker in _markers)
			marker.Free();

		_markers.Clear();
	}

	private void AddMarker(Coord cell)
	{
		var root = new Node3D
		{
			Position = WorldMapping.ToWorld(cell),
		};

		var arrow = CreateArrow();
		arrow.Position = new Vector3(0f, ArrowHoverHeight, 0f);
		arrow.Rotation = new Vector3(Mathf.Pi, 0f, 0f);
		root.AddChild(arrow);

		var bob = arrow.CreateTween().SetLoops();
		bob.TweenProperty(arrow, "position:y", ArrowHoverHeight - ArrowBobAmplitude, 0.55)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		bob.TweenProperty(arrow, "position:y", ArrowHoverHeight + ArrowBobAmplitude * 0.25f, 0.55)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);

		AddChild(root);
		_markers.Add(root);
	}

	private static Node3D CreateArrow()
	{
		var arrow = new Node3D { Name = "Arrow" };
		var material = CreateMaterial(new Color(1f, 0.32f, 0.22f, 0.95f), 2.4f);

		var shaft = new MeshInstance3D
		{
			Mesh = new CylinderMesh
			{
				TopRadius = 0.12f,
				BottomRadius = 0.12f,
				Height = 0.9f,
			},
			Position = new Vector3(0f, 0.45f, 0f),
			MaterialOverride = material,
		};
		PresentationLayers.MarkUx(shaft);
		arrow.AddChild(shaft);

		var head = new MeshInstance3D
		{
			Mesh = new CylinderMesh
			{
				TopRadius = 0f,
				BottomRadius = 0.42f,
				Height = 0.62f,
			},
			Position = new Vector3(0f, 1.2f, 0f),
			MaterialOverride = material,
		};
		PresentationLayers.MarkUx(head);
		arrow.AddChild(head);

		return arrow;
	}

	private static StandardMaterial3D CreateMaterial(Color color, float emissionEnergy) =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color with { A = 1f },
			EmissionEnergyMultiplier = emissionEnergy,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
		};
}
