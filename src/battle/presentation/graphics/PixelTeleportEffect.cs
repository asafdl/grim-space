using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class PixelTeleportEffect : Node
{
	public const double FadeSeconds = 0.325;
	public const double HiddenSeconds = 0.06;
	public const double DurationSeconds = FadeSeconds * 2 + HiddenSeconds;
	private readonly List<(MeshInstance3D Mesh, int Surface, Material? Original, ShaderMaterial Effect)> _surfaces = [];
	private Tween? _tween;

	public void Play(Node3D hull, Action relocate)
	{
		Stop();
		var shader = GD.Load<Shader>("res://assets/shaders/pixel_teleport.gdshader")
			?? throw new InvalidOperationException("Pixel teleport shader could not be loaded.");
		var surfaces = hull.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
			.Prepend(hull as MeshInstance3D).OfType<MeshInstance3D>();
		foreach (var mesh in surfaces)
		{
			if (mesh.Mesh is null)
				continue;
			for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
			{
				if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D original)
					throw new InvalidOperationException($"Pixel teleport requires a standard material on {mesh.Name}.");
				var effect = new ShaderMaterial { Shader = shader };
				effect.SetShaderParameter("albedo_color", original.AlbedoColor);
				effect.SetShaderParameter("use_albedo_texture", original.AlbedoTexture is not null);
				if (original.AlbedoTexture is not null)
					effect.SetShaderParameter("albedo_texture", original.AlbedoTexture);
				effect.SetShaderParameter("metallic_amount", original.Metallic);
				effect.SetShaderParameter("roughness_amount", original.Roughness);
				_surfaces.Add((mesh, surface, mesh.GetSurfaceOverrideMaterial(surface), effect));
			}
		}
		if (_surfaces.Count == 0)
			throw new InvalidOperationException("Pixel teleport has no mesh surfaces.");
		foreach (var surface in _surfaces)
			surface.Mesh.SetSurfaceOverrideMaterial(surface.Surface, surface.Effect);
		_tween = CreateTween();
		_tween.TweenMethod(Callable.From<float>(SetDissolve), 0f, 1f, FadeSeconds);
		_tween.TweenInterval(HiddenSeconds);
		_tween.TweenCallback(Callable.From(relocate));
		_tween.TweenMethod(Callable.From<float>(SetDissolve), 1f, 0f, FadeSeconds);
		_tween.TweenCallback(Callable.From(Stop));
	}

	public void Stop()
	{
		_tween?.Kill();
		_tween = null;
		foreach (var surface in _surfaces)
		{
			if (IsInstanceValid(surface.Mesh))
				surface.Mesh.SetSurfaceOverrideMaterial(surface.Surface, surface.Original);
		}
		_surfaces.Clear();
	}

	public void SetPaused(bool paused)
	{
		if (paused)
			_tween?.Pause();
		else
			_tween?.Play();
	}

	public override void _ExitTree() => Stop();

	private void SetDissolve(float amount)
	{
		foreach (var surface in _surfaces)
			surface.Effect.SetShaderParameter("dissolve", amount);
	}
}
