using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Short-lived replay burst animations — transient only, nothing persists on screen.
/// Weapon dots reuse <see cref="WeaponPreviewMaterials"/> from planning previews.
/// </summary>
public partial class HazardBurstView : Node3D
{
	private const float DotRadius = WorldMapping.CellSize * 0.22f;
	private const int ShotDotCount = 4;
	private const int RadialRingCount = 4;

	public void Clear()
	{
		// Transient VFX free themselves; nothing to retain between turns.
	}

	public void PlayShotBurst(
		Coord origin,
		Vector3 direction,
		float reachCells,
		Color tint,
		double duration)
	{
		var worldOrigin = WorldMapping.ToWorld(origin);
		var dir = direction.Normalized();
		var maxReach = reachCells * WorldMapping.CellSize;
		var mesh = CreateDotMesh();

		for (var i = 0; i < ShotDotCount; i++)
		{
			var delay = duration * 0.07 * i;
			var reach = maxReach * (i + 1) / ShotDotCount;
			var endScale = 0.55f + i * 0.28f;
			SpawnExpandingDot(mesh, worldOrigin, tint, duration, delay, endScale,
				endPos: worldOrigin + dir * reach);
		}
	}

	public void PlayRadialBurst(
		Coord origin,
		int radiusCells,
		Color tint,
		double duration)
	{
		var worldOrigin = WorldMapping.ToWorld(origin);
		var maxScale = radiusCells * WorldMapping.CellSize / DotRadius;
		var mesh = CreateDotMesh();

		for (var i = 0; i < RadialRingCount; i++)
		{
			var delay = duration * 0.09 * i;
			var endScale = maxScale * (i + 1) / RadialRingCount;
			SpawnExpandingDot(mesh, worldOrigin, tint, duration, delay, endScale);
		}
	}

	private void SpawnExpandingDot(
		SphereMesh mesh,
		Vector3 startPos,
		Color tint,
		double duration,
		double delay,
		float endScale,
		Vector3? endPos = null)
	{
		var material = WeaponPreviewMaterials.CreateDotted(tint);
		var marker = new MeshInstance3D
		{
			Mesh = mesh,
			MaterialOverride = material,
			Position = startPos,
			Scale = Vector3.Zero,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(marker);
		AddChild(marker);

		var moveDuration = duration * 0.55;
		var tween = CreateTween();
		tween.TweenInterval(delay);
		tween.TweenProperty(marker, "scale", Vector3.One * endScale, moveDuration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);

		if (endPos is Vector3 target && target != startPos)
			tween.Parallel().TweenProperty(marker, "position", target, moveDuration)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);

		PresentationTween.ParallelFadeShaderStrength(
			tween,
			material,
			1f,
			0f,
			duration * 0.35,
			delay + moveDuration * 0.35);
		PresentationTween.ChainFree(tween, marker);
	}

	private static SphereMesh CreateDotMesh() =>
		new()
		{
			Radius = DotRadius,
			Height = DotRadius * 2f,
		};
}
