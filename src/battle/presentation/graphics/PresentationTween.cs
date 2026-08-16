using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Thin helpers around Godot's <see cref="Tween"/> for short-lived presentation effects.
/// Prefer engine tweens over custom animation loops; no external VFX libraries in this project.
/// </summary>
internal static class PresentationTween
{
	public static Tween FloatAndFree(
		Node owner,
		Node3D node,
		Vector3 endOffset,
		double duration,
		double fadeDelay = 0)
	{
		var tween = owner.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(node, "position", node.Position + endOffset, duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		if (node is GeometryInstance3D instance)
			tween.TweenProperty(instance, "modulate", Colors.Transparent, duration * 0.82)
				.SetDelay(fadeDelay > 0 ? fadeDelay : duration * 0.2);

		tween.Chain().TweenCallback(Callable.From(node.QueueFree));
		return tween;
	}

	public static void ParallelFadeShaderStrength(
		Tween tween,
		ShaderMaterial material,
		float from,
		float to,
		double duration,
		double delay = 0)
	{
		var step = tween.Parallel().TweenMethod(
			Callable.From<float>(strength => material.SetShaderParameter("strength", strength)),
			from,
			to,
			duration);
		if (delay > 0)
			step.SetDelay(delay);
	}

	public static void ChainFree(Tween tween, Node target) =>
		tween.Chain().TweenCallback(Callable.From(target.QueueFree));
}
