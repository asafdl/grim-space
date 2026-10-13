using System;
using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class VoidBombEffect : Node3D
{
	private const string ScenePath =
		"res://assets/vfx/void_bomb_pulse_detonation.glb";
	private const string TextScenePath =
		"res://assets/vfx/voosh-godot.glb";
	private const string TextNodeName = "VooshText";

	// Approximate maximum ray reach in this particular GLB.
	private const float AuthoredRadius = 2.4f;
	private const float TextWidth = AuthoredRadius * 1.26f;
	private const double TextAppearDelay = 0.15;
	private const double TextFadeInDuration = 0.06;
	private const double TextFadeStart = 0.6;
	private const double TextFadeOutDuration = 0.2;
	private const float AnimationSpeed = 0.82f;
	private const double FlashDuration = 0.18;

	private static PackedScene? _scene;
	private static PackedScene? _textScene;

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
		var text = AddText(effect);

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
		AnimateText(effect, text);
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

	private static MeshInstance3D AddText(VoidBombEffect effect)
	{
		_textScene ??= GD.Load<PackedScene>(TextScenePath)
			?? throw new InvalidOperationException(
				$"Could not load void bomb text '{TextScenePath}'.");
		var root = _textScene.Instantiate<Node3D>();
		effect.AddChild(root);

		var text = root.FindChild(
			TextNodeName,
			recursive: true,
			owned: false) as MeshInstance3D
			?? throw new InvalidOperationException(
				$"Void bomb text '{TextScenePath}' has no '{TextNodeName}' mesh.");
		var bounds = text.Mesh?.GetAabb()
			?? throw new InvalidOperationException(
				$"Void bomb text '{TextNodeName}' has no mesh.");
		if (bounds.Size.X <= 0f)
			throw new InvalidOperationException(
				$"Void bomb text '{TextNodeName}' has no width.");

		var scale = TextWidth / bounds.Size.X;
		text.Position = new Vector3(0f, AuthoredRadius * 0.45f, 0f);
		text.Scale = Vector3.One * scale;
		text.Transparency = 1f;
		text.CastShadow =
			GeometryInstance3D.ShadowCastingSetting.Off;
		PresentationLayers.MarkWorld(text);

		for (var surface = 0; surface < text.Mesh.GetSurfaceCount(); surface++)
		{
			var source = text.GetActiveMaterial(surface) as BaseMaterial3D
				?? throw new InvalidOperationException(
					$"Void bomb text surface {surface} has no 3D material.");
			var material = (BaseMaterial3D)source.Duplicate();
			material.BillboardMode =
				BaseMaterial3D.BillboardModeEnum.Enabled;
			material.BillboardKeepScale = true;
			text.SetSurfaceOverrideMaterial(surface, material);
		}

		return text;
	}

	private static void AnimateText(
		VoidBombEffect effect,
		MeshInstance3D text)
	{
		var fullScale = text.Scale;
		text.Scale = fullScale * 0.85f;

		var tween = effect.CreateTween();
		tween.TweenInterval(TextAppearDelay);
		tween.TweenProperty(
			text,
			"transparency",
			0f,
			TextFadeInDuration);
		tween.Parallel().TweenProperty(
			text,
			"scale",
			fullScale,
			TextFadeInDuration);
		tween.TweenInterval(
			TextFadeStart
			- TextAppearDelay
			- TextFadeInDuration);
		tween.TweenProperty(
			text,
			"transparency",
			1f,
			TextFadeOutDuration);
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
