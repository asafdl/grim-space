using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class MovementAfterimageView : Node3D
{
	private const int MaxEchoes = 6;
	private const float SpacingCells = 0.75f;
	private const int CurveSubdivisions = 32;
	private const int SmearSlices = 16;
	private static readonly Shader GhostShader = new()
	{
		Code = """
			shader_type spatial;
			render_mode unshaded, depth_draw_never, shadows_disabled;
			uniform vec4 tint : source_color;
			void fragment() {
				float rim = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 2.0);
				ALBEDO = tint.rgb * (2.2 + rim * 2.0);
				ALPHA = tint.a;
			}
			""",
	};
	private readonly List<(EType Type, UnitView View, ShaderMaterial Material,
		ShaderMaterial SmearMaterial, MultiMeshInstance3D[] Smears)> _echoes = [];
	private readonly Shader _smearShader = new()
	{
		Code = """
			shader_type spatial;
			render_mode unshaded, depth_draw_never, shadows_disabled;
			uniform vec4 tint : source_color;
			varying float opacity;
			void vertex() {
				opacity = INSTANCE_CUSTOM.r;
			}
			void fragment() {
				float rim = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 2.0);
				ALBEDO = tint.rgb * (2.2 + rim * 2.0);
				ALPHA = tint.a * opacity;
			}
			""",
	};
	private readonly List<(EType Type, UnitView View, ShaderMaterial Material)> _origins = [];

	public void ApplyOrigins(IReadOnlyList<UnitDisplayState> origins)
	{
		for (var i = 0; i < origins.Count; i++)
		{
			if (i == _origins.Count)
			{
				var view = new UnitView { Name = $"AbilityOrigin{i}" };
				view.Bind(origins[i].ToState(), Colors.White);
				AddChild(view);
				view.SetProcess(false);
				_origins.Add((origins[i].Type, view, CreateMaterial(Colors.White)));
			}
			else if (_origins[i].Type != origins[i].Type)
			{
				_origins[i].View.QueueFree();
				var view = new UnitView { Name = $"AbilityOrigin{i}" };
				view.Bind(origins[i].ToState(), Colors.White);
				AddChild(view);
				view.SetProcess(false);
				_origins[i] = (origins[i].Type, view, CreateMaterial(Colors.White));
			}
			var (_, origin, material) = _origins[i];
			var progress = (i + 1f) / origins.Count;
			material.SetShaderParameter("tint", new Color(0.25f, 0.32f, 0.4f, 0.1f)
				.Lerp(new Color(0.65f, 0.73f, 0.8f, 0.35f), progress));
			origin.Present(origins[i].ToState(), UnitVisualState.Ghost);
			StyleHull(origin, material);
		}
		for (var i = origins.Count; i < _origins.Count; i++)
			_origins[i].View.HideVisual();
	}

	public void Apply(
		IReadOnlyList<MoveCheckpoint> checkpoints,
		UnitDisplayState template)
	{
		var curve = new List<(Vector3 Position, Basis Basis, float Distance)>();
		var distances = new float[checkpoints.Count];
		Vector3? previousTangent = null;
		if (checkpoints.Count > 0)
			curve.Add((WorldMapping.ToWorld(checkpoints[0].Position),
				ToBasis(checkpoints[0].Basis), 0f));
		for (var i = 1; i < checkpoints.Count; i++)
		{
			var from = checkpoints[i - 1];
			var to = checkpoints[i];
			var start = WorldMapping.ToWorld(from.Position);
			var end = WorldMapping.ToWorld(to.Position);
			var startTangent = previousTangent ?? (end - start) * 0.65f;
			var endTangent = UnitView.MovementEndTangent(
				end - start, end, i + 1 < checkpoints.Count ? checkpoints[i + 1].Position : null);
			previousTangent = endTangent;
			var startBasis = ToBasis(from.Basis);
			var endBasis = ToBasis(to.Basis);
			// Measure the replay curve so echoes remain evenly spaced through turns.
			for (var step = 1; step <= CurveSubdivisions; step++)
			{
				var weight = (float)step / CurveSubdivisions;
				var position = UnitView.Hermite(start, end, startTangent, endTangent, weight);
				var previous = curve[^1];
				curve.Add((position, startBasis.Slerp(endBasis, weight),
					previous.Distance + previous.Position.DistanceTo(position)));
			}
			distances[i] = curve[^1].Distance;
		}
		var total = curve.Count > 0 ? curve[^1].Distance : 0f;
		var spacing = SpacingCells * WorldMapping.CellSize;
		var samples = new SortedSet<float>();
		if (total > spacing)
		{
			var count = Mathf.Min(MaxEchoes, Mathf.CeilToInt(total / spacing) - 1);
			for (var i = 1; i <= count; i++)
				samples.Add(total * i / (count + 1));

			// Give bends priority over evenly spaced samples without exceeding the echo budget.
			for (var i = 1; i < checkpoints.Count - 1; i++)
			{
				var before = checkpoints[i].Position - checkpoints[i - 1].Position;
				var after = checkpoints[i + 1].Position - checkpoints[i].Position;
				if (before == Coord.Zero || after == Coord.Zero || before == after)
					continue;
				var nearest = samples.MinBy(distance => Mathf.Abs(distance - distances[i]));
				samples.Remove(nearest);
				samples.Add(distances[i]);
			}
		}

		while (_echoes.Count < samples.Count)
			_echoes.Add(CreateEcho(template, _echoes.Count));
		for (var i = 0; i < samples.Count; i++)
		{
			if (_echoes[i].Type == template.Type)
				continue;

			_echoes[i].View.QueueFree();
			_echoes[i] = CreateEcho(template, i);
		}
		for (var i = 0; i < _echoes.Count; i++)
			_echoes[i].View.Visible = i < samples.Count;

		var index = 0;
		foreach (var distance in samples)
		{
			var segment = 1;
			while (segment < curve.Count - 1 && curve[segment].Distance < distance)
				segment++;
			var from = curve[segment - 1];
			var to = curve[segment];
			var span = to.Distance - from.Distance;
			var weight = span > 0f ? (distance - from.Distance) / span : 1f;
			var progress = distance / total;
			var (_, view, material, smearMaterial, smears) = _echoes[index++];
			view.Present(template.ToState(), UnitVisualState.Ghost);
			view.Position = from.Position.Lerp(to.Position, weight);
			view.Basis = from.Basis.Slerp(to.Basis, weight);
			var tint = new Color(0.25f, 0.32f, 0.4f, 0.1f)
				.Lerp(new Color(0.85f, 0.92f, 1f, 0.55f), progress);
			material.SetShaderParameter("tint", tint);
			StyleHull(view, material);
			smearMaterial.SetShaderParameter("tint", tint);
			var direction = (to.Position - from.Position).Normalized();
			var length = Mathf.Min(WorldMapping.CellSize * 0.25f, total / (samples.Count + 1) * 0.4f);
			foreach (var smear in smears)
			{
				// Present replaces materials on every hull descendant, including the smear instances.
				smear.MaterialOverride = smearMaterial;
				var localOffset = smear.GlobalBasis.Inverse() * (GlobalBasis * -direction * length);
				for (var slice = 0; slice < SmearSlices; slice++)
				{
					var fade = (slice + 1f) / SmearSlices;
					smear.Multimesh.SetInstanceTransform(slice,
						new Transform3D(Basis.Identity, localOffset * fade));
					smear.Multimesh.SetInstanceCustomData(slice,
						new Color(1.3f / SmearSlices * (1f - fade) * (1f - fade), 0f, 0f, 0f));
				}
			}
		}
	}

	private static Basis ToBasis(GridBasis basis) =>
		new(new Vector3(basis.Right.X, basis.Right.Y, basis.Right.Z),
			new Vector3(basis.Up.X, basis.Up.Y, basis.Up.Z),
			new Vector3(basis.Forward.X, basis.Forward.Y, basis.Forward.Z));

	private static ShaderMaterial CreateMaterial(Color color)
	{
		var material = new ShaderMaterial { Shader = GhostShader };
		material.SetShaderParameter("tint", color);
		return material;
	}

	private (EType Type, UnitView View, ShaderMaterial Material,
		ShaderMaterial SmearMaterial, MultiMeshInstance3D[] Smears) CreateEcho(
			UnitDisplayState template,
			int index)
	{
		var view = new UnitView { Name = $"Afterimage{index}" };
		view.Bind(template.ToState(), Colors.White);
		AddChild(view);
		view.SetProcess(false);
		var material = CreateMaterial(Colors.White);
		StyleHull(view, material);
		var smearMaterial = new ShaderMaterial { Shader = _smearShader, RenderPriority = -1 };
		var smears = view.GetNode<Node3D>("IdleMotion/VisualRoot")
			.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
			.Where(mesh => mesh.MaterialOverride == material && mesh.Mesh is not null)
			.Select(mesh =>
			{
				var smear = new MultiMeshInstance3D
				{
					Name = "MotionSmear",
					CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
					MaterialOverride = smearMaterial,
					Multimesh = new MultiMesh
					{
						TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
						UseCustomData = true,
						Mesh = mesh.Mesh,
						InstanceCount = SmearSlices,
					},
				};
				mesh.AddChild(smear);
				return smear;
			}).ToArray();
		return (template.Type, view, material, smearMaterial, smears);
	}

	private static void StyleHull(UnitView view, Material material)
	{
		var root = view.GetNode<Node3D>("IdleMotion/VisualRoot");
		foreach (var child in root.GetChildren().OfType<Node3D>())
		{
			var hull = child is MeshInstance3D
				&& !child.Name.ToString().StartsWith("Shield", StringComparison.Ordinal);
			if (!hull)
			{
				child.Hide();
				continue;
			}
			foreach (var mesh in child.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>().Prepend((MeshInstance3D)child))
				mesh.MaterialOverride = material;
		}
	}
}
