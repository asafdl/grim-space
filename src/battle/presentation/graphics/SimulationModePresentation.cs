using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed class SimulationModePresentation
{
	private static readonly Shader ShipShader = new()
	{
		Code =
			"""
			shader_type spatial;
			render_mode unshaded;

			uniform float intensity = 1.0;
			uniform vec3 tint : source_color = vec3(1.0, 0.001, 0.003);
			void fragment() {
				float facing = max(dot(normalize(NORMAL), normalize(VIEW)), 0.0);
				vec3 facet_normal = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
				float facet = abs(dot(facet_normal, normalize(vec3(0.4, 0.7, 0.5))));
				float shape = 0.65 + floor(facet * 4.0) * 0.05 + facing * 0.15;
				ALBEDO = tint * intensity * shape;
			}
			""",
	};
	private static readonly Shader AsteroidShader = new()
	{
		Code =
			"""
			shader_type spatial;
			render_mode unshaded, wireframe, shadows_disabled;

			void fragment() {
				ALBEDO = vec3(0.7, 0.75, 0.77);
			}
			""",
	};
	private static readonly Color BlueprintMovementColor = new(0.25f, 0.85f, 0.95f);
	private static readonly Color EnemyTint = new(1f, 0.001f, 0.003f);
	private static readonly Color TargetTint = new(1f, 0.4f, 0.015f);
	private static readonly Color PlayerTint = new(0.03f, 0.32f, 1f);
	private static readonly Color AllyTint = new(0.015f, 1f, 0.12f);
	private static readonly Godot.Environment.GlowBlendModeEnum SimulationGlowBlend =
		Godot.Environment.GlowBlendModeEnum.Additive;

	private readonly ShaderMaterial _backdropWireframe = new() { Shader = AsteroidShader };
	private readonly ShaderMaterial _enemyGlow = CreateShipMaterial(2.2f, EnemyTint);
	private readonly ShaderMaterial _targetGlow = CreateShipMaterial(3.5f, TargetTint);
	private readonly ShaderMaterial _playerGlow = CreateShipMaterial(3.5f, PlayerTint);
	private readonly ShaderMaterial _allyGlow = CreateShipMaterial(2.2f, AllyTint);
	private readonly Dictionary<MeshInstance3D, Material?> _unitMaterials = [];
	private readonly Dictionary<MeshInstance3D, Material?> _asteroidMaterials = [];
	private readonly Dictionary<StandardMaterial3D, Color> _movementColors = [];
	private readonly ShaderMaterial _backdropTint;
	private readonly WorldEnvironment _worldEnvironment;
	private readonly Godot.Environment _normalEnvironment;
	private readonly Godot.Environment _simulationEnvironment;

	public SimulationModePresentation(
		ShaderMaterial backdropTint,
		WorldEnvironment worldEnvironment,
		IEnumerable<MeshInstance3D> asteroidVisuals,
		IEnumerable<StandardMaterial3D> movementMaterials)
	{
		_backdropTint = backdropTint;
		_worldEnvironment = worldEnvironment;
		_normalEnvironment = worldEnvironment.Environment
			?? throw new InvalidOperationException("Simulation presentation requires a world environment.");
		_simulationEnvironment = (Godot.Environment)_normalEnvironment.Duplicate(true);
		_simulationEnvironment.GlowIntensity = 0.9f;
		_simulationEnvironment.GlowBlendMode = SimulationGlowBlend;
		_simulationEnvironment.GlowStrength = 1.3f;
		_simulationEnvironment.GlowHdrThreshold = 1.1f;
		_simulationEnvironment.SetGlowLevel(0, 1f);
		_simulationEnvironment.SetGlowLevel(1, 0.6f);

		foreach (var visual in asteroidVisuals)
			_asteroidMaterials.Add(visual, visual.MaterialOverride);
		foreach (var material in movementMaterials)
			_movementColors.Add(material, material.AlbedoColor);
	}

	public static ShaderMaterial CreateBackdropTintMaterial() =>
		new()
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type canvas_item;
					render_mode unshaded;

					uniform bool blueprint_enabled = false;

					void fragment() {
						vec4 source = texture(TEXTURE, UV);
						if (blueprint_enabled) {
							float luminance = dot(source.rgb, vec3(0.2126, 0.7152, 0.0722));
							vec3 ink = vec3(0.008, 0.035, 0.095);
							vec3 cyan = vec3(0.22, 0.8, 0.88);
							COLOR = vec4(mix(ink, cyan, luminance), 1.0);
						} else {
							COLOR = source;
						}
					}
					""",
			},
		};

	public void Apply(
		bool enabled,
		IReadOnlyDictionary<string, UnitView> unitViews,
		IReadOnlyDictionary<string, ETeam> teams,
		string playerId,
		string? targetId)
	{
		if (!enabled)
		{
			RestoreUnitMaterials();
			_backdropTint.SetShaderParameter("blueprint_enabled", false);
			RestoreStaticMaterials();
			_worldEnvironment.Environment = _normalEnvironment;
			return;
		}

		_backdropTint.SetShaderParameter("blueprint_enabled", true);
		_worldEnvironment.Environment = _simulationEnvironment;
		foreach (var (visual, _) in _asteroidMaterials)
		{
			if (GodotObject.IsInstanceValid(visual))
				visual.MaterialOverride = _backdropWireframe;
		}
		foreach (var (material, original) in _movementColors)
			material.AlbedoColor = new Color(
				BlueprintMovementColor.R,
				BlueprintMovementColor.G,
				BlueprintMovementColor.B,
				original.A);

		var activeMeshes = new HashSet<MeshInstance3D>();
		foreach (var (id, unit) in unitViews)
		{
			if (!teams.TryGetValue(id, out var team)
				|| (id != playerId && team is not (ETeam.Player or ETeam.Enemy)))
				continue;

			var material = id == playerId ? _playerGlow
				: team == ETeam.Player ? _allyGlow
				: id == targetId ? _targetGlow
				: _enemyGlow;
			var visualRoot = unit.GetNodeOrNull<Node3D>("IdleMotion/VisualRoot");
			if (visualRoot is null)
				continue;

			foreach (var hull in visualRoot.GetChildren().OfType<MeshInstance3D>()
				.Where(mesh => !mesh.Name.ToString().StartsWith("Shield", StringComparison.Ordinal)))
			{
				foreach (var mesh in hull.FindChildren("*", "MeshInstance3D", true, false)
					.OfType<MeshInstance3D>().Prepend(hull))
				{
					_unitMaterials.TryAdd(mesh, mesh.MaterialOverride);
					mesh.MaterialOverride = material;
					activeMeshes.Add(mesh);
				}
			}
		}

		foreach (var (mesh, original) in _unitMaterials
			.Where(pair => !activeMeshes.Contains(pair.Key))
			.ToArray())
		{
			if (GodotObject.IsInstanceValid(mesh))
				mesh.MaterialOverride = original;
			_unitMaterials.Remove(mesh);
		}
	}

	public void Dispose()
	{
		RestoreUnitMaterials();
		RestoreStaticMaterials();
		_backdropTint.SetShaderParameter("blueprint_enabled", false);
		if (GodotObject.IsInstanceValid(_worldEnvironment))
			_worldEnvironment.Environment = _normalEnvironment;
	}

	private void RestoreUnitMaterials()
	{
		foreach (var (visual, original) in _unitMaterials)
		{
			if (GodotObject.IsInstanceValid(visual))
				visual.MaterialOverride = original;
		}
		_unitMaterials.Clear();
	}

	private void RestoreStaticMaterials()
	{
		foreach (var (visual, original) in _asteroidMaterials)
		{
			if (GodotObject.IsInstanceValid(visual))
				visual.MaterialOverride = original;
		}
		foreach (var (material, original) in _movementColors)
			material.AlbedoColor = original;
	}

	private static ShaderMaterial CreateShipMaterial(float intensity, Color tint)
	{
		var material = new ShaderMaterial { Shader = ShipShader };
		material.SetShaderParameter("intensity", intensity);
		material.SetShaderParameter("tint", tint);
		return material;
	}
}
