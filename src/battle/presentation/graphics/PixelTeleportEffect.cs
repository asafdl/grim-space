using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class PixelTeleportEffect : Node
{
	public const double FadeSeconds = 0.325;
	public const double HiddenSeconds = 0.06;
	public const double DurationSeconds = FadeSeconds * 2 + HiddenSeconds;
	private readonly List<(MeshInstance3D Mesh, int Surface, Material? Original, ShaderMaterial Effect)> _surfaces = [];
	private readonly List<(MeshInstance3D Mesh, Material Original, ShaderMaterial Effect)> _overrides = [];
	private readonly List<ShaderMaterial> _shields = [];
	private Tween? _tween;

	public void Play(Node3D hull, Action relocate, IEnumerable<MeshInstance3D>? extraMeshes = null)
	{
		Stop();
		var shader = GD.Load<Shader>("res://assets/shaders/pixel_teleport.gdshader")
			?? throw new InvalidOperationException("Pixel teleport shader could not be loaded.");
		var surfaces = hull.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
			.Prepend(hull as MeshInstance3D).OfType<MeshInstance3D>()
			.Concat(extraMeshes ?? []);
		foreach (var mesh in surfaces)
		{
			if (mesh.Mesh is null)
				continue;
			if (mesh.MaterialOverride is not null)
			{
				if (mesh.MaterialOverride is ShaderMaterial shield && shield.Shader == ShieldFaceMaterials.Shader)
				{
					_shields.Add(shield);
					continue;
				}
				_overrides.Add((mesh, mesh.MaterialOverride, CreateEffectMaterial(shader, mesh, mesh.MaterialOverride)));
				continue;
			}
			for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
			{
				var active = mesh.GetActiveMaterial(surface)
					?? throw new InvalidOperationException($"Pixel teleport requires a material on {mesh.Name}.");
				_surfaces.Add((
					mesh,
					surface,
					mesh.GetSurfaceOverrideMaterial(surface),
					CreateEffectMaterial(shader, mesh, active)));
			}
		}
		if (_surfaces.Count == 0 && _overrides.Count == 0 && _shields.Count == 0)
			throw new InvalidOperationException("Pixel teleport has no mesh surfaces.");
		foreach (var over in _overrides)
			over.Mesh.MaterialOverride = over.Effect;
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
		foreach (var shield in _shields)
		{
			if (IsInstanceValid(shield))
				shield.SetShaderParameter("dissolve", 0f);
		}
		_shields.Clear();
		foreach (var over in _overrides)
		{
			if (IsInstanceValid(over.Mesh))
				over.Mesh.MaterialOverride = over.Original;
		}
		_overrides.Clear();
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
		foreach (var shield in _shields)
			shield.SetShaderParameter("dissolve", amount);
		foreach (var over in _overrides)
			over.Effect.SetShaderParameter("dissolve", amount);
		foreach (var surface in _surfaces)
			surface.Effect.SetShaderParameter("dissolve", amount);
	}

	private static ShaderMaterial CreateEffectMaterial(Shader shader, MeshInstance3D mesh, Material active)
	{
		if (active is not StandardMaterial3D original)
			throw new InvalidOperationException($"Pixel teleport requires a standard material on {mesh.Name}.");
		var effect = new ShaderMaterial { Shader = shader };
		var albedo = original.Transparency == BaseMaterial3D.TransparencyEnum.Disabled
			? new Color(original.AlbedoColor, 1f)
			: original.AlbedoColor;
		effect.SetShaderParameter("albedo_color", albedo);
		effect.SetShaderParameter("use_albedo_texture", original.AlbedoTexture is not null);
		if (original.AlbedoTexture is not null)
			effect.SetShaderParameter("albedo_texture", original.AlbedoTexture);
		effect.SetShaderParameter("metallic_amount", original.Metallic);
		effect.SetShaderParameter("roughness_amount", original.Roughness);
		effect.SetShaderParameter(
			"unshaded",
			original.ShadingMode == BaseMaterial3D.ShadingModeEnum.Unshaded);
		return effect;
	}
}
