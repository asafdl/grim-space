using System;
using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class VoidBombEffect : Node3D
{
	private const string ScenePath =
		"res://assets/vfx/void_bomb_pulse_detonation.glb";

	// Approximate maximum ray reach in this particular GLB.
	private const float AuthoredRadius = 2.4f;
	private const float AnimationSpeed = 0.82f;
	private const double FlashDuration = 0.18;

	private static PackedScene? _scene;

	public static double Play(
		Node parent,
		Vector3 worldPosition,
		float worldRadius)
	{
		_scene ??= GD.Load<PackedScene>(ScenePath);

		var effect = new VoidBombEffect
		{
			Name = "VoidBombEffect",
			Visible = false
		};

		var model = _scene.Instantiate<Node3D>();
		effect.AddChild(model);

		PrepareMeshes(model);

		var player = FindAnimationPlayer(model);
		if (player == null)
		{
			effect.Free();
			throw new InvalidOperationException(
				"Void bomb GLB has no AnimationPlayer.");
		}

		var animationName = FindDetonateAnimation(player);
		if (animationName == null)
		{
			effect.Free();
			throw new InvalidOperationException(
				"Void bomb GLB has no Detonate animation. " +
				"Enable animation import in Godot.");
		}

		var animation = player.GetAnimation(animationName);
		if (animation.LoopMode != Animation.LoopModeEnum.None)
		{
			effect.Free();
			throw new InvalidOperationException(
				"Set the imported Detonate animation to not loop.");
		}

		parent.AddChild(effect);

		effect.TopLevel = true;
		effect.GlobalPosition = worldPosition;
		effect.Scale = Vector3.One *
			(Mathf.Max(worldRadius, 0.01f) / AuthoredRadius);
		AddOriginFlash(effect);
		PresentationSfx.PlayWorldOneShot(
			effect,
			Vector3.Zero,
			PresentationSfx.VoidBombExplosionPath);

		player.AnimationFinished += _ => effect.QueueFree();

		player.Play(
			animationName,
			customBlend: 0,
			customSpeed: AnimationSpeed);
		player.Advance(0);

		effect.Visible = true;

		return animation.Length / AnimationSpeed;
	}

	private static void AddOriginFlash(VoidBombEffect effect)
	{
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			NoDepthTest = true,
			AlbedoColor = Colors.White,
			EmissionEnabled = true,
			Emission = Colors.White,
			EmissionEnergyMultiplier = 5f,
		};
		var flash = new MeshInstance3D
		{
			Name = "VoidBombOriginFlash",
			Mesh = new SphereMesh
			{
				Radius = 0.18f,
				Height = 0.36f,
				RadialSegments = 16,
				Rings = 8,
			},
			Scale = Vector3.One * 0.25f,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		effect.AddChild(flash);

		var tween = effect.CreateTween();
		tween.SetParallel();
		tween.TweenProperty(
			flash,
			"scale",
			Vector3.One * 2.8f,
			FlashDuration);
		tween.TweenMethod(
			Callable.From<float>(alpha =>
				material.AlbedoColor = Colors.White with { A = alpha }),
			1f,
			0f,
			FlashDuration);
		tween.TweenMethod(
			Callable.From<float>(energy =>
				material.EmissionEnergyMultiplier = energy),
			5f,
			0f,
			FlashDuration);
	}

	private static void PrepareMeshes(Node node)
	{
		if (node is MeshInstance3D mesh)
		{
			PresentationLayers.MarkWorld(mesh);
			mesh.CastShadow =
				GeometryInstance3D.ShadowCastingSetting.Off;
		}

		foreach (Node child in node.GetChildren())
			PrepareMeshes(child);
	}

	private static AnimationPlayer? FindAnimationPlayer(Node node)
	{
		if (node is AnimationPlayer player)
			return player;

		foreach (Node child in node.GetChildren())
		{
			var found = FindAnimationPlayer(child);
			if (found != null)
				return found;
		}

		return null;
	}

	private static string? FindDetonateAnimation(AnimationPlayer player)
	{
		foreach (string name in player.GetAnimationList())
		{
			if (name == "Detonate" ||
				name.EndsWith("/Detonate", StringComparison.Ordinal))
				return name;
		}

		return null;
	}
}
