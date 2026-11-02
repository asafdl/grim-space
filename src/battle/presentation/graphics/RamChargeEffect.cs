using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class RamChargeEffect : Node3D
{
	public const double RevealSeconds = 0.6;
	public const double HoldSeconds = 0.25;
	public const double DashSeconds = 0.1;
	public const double DurationSeconds = RevealSeconds + HoldSeconds + DashSeconds;

	private readonly List<MeshInstance3D> _strokes = [];
	private readonly List<MeshInstance3D> _exhaust = [];

	public void Build(Aabb bounds)
	{
		var center = bounds.GetCenter();
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(1.4f, 2.2f, 2.8f),
			EmissionEnabled = true,
			Emission = new Color(0.5f, 0.8f, 1f),
			EmissionEnergyMultiplier = 2f,
		};
		for (var i = 0; i < 6; i++)
		{
			var angle = Mathf.Tau * i / 6f;
			var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
			var radius = new Vector3(
				bounds.Size.X * 0.35f, bounds.Size.Y * 0.35f + 0.08f, 0);
			var points = new Vector3[21];
			for (var j = 0; j < points.Length; j++)
			{
				var t = j / (float)(points.Length - 1);
				points[j] = new Vector3(center.X, center.Y, bounds.Position.Z - 0.08f)
					+ radial * radius * (1f + 0.12f * Mathf.Sin(t * Mathf.Pi))
					+ Vector3.Forward * (bounds.Size.Z * t * (0.9f + i % 3 * 0.12f));
			}
			var stroke = new MeshInstance3D
			{
				Name = $"SpeedStroke{i}",
				Mesh = CreateStroke(points),
				MaterialOverride = material,
				Visible = false,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkWorld(stroke);
			AddChild(stroke);
			_strokes.Add(stroke);
		}
		BuildExhaust(bounds, subtle: false);
		SetCharge(0);
	}

	public void BuildEngineGlow(Aabb bounds)
	{
		BuildExhaust(bounds, subtle: true);
		SetCharge(0);
	}

	private void BuildExhaust(Aabb bounds, bool subtle)
	{
		var center = bounds.GetCenter();
		foreach (var side in new[] { -1f, 1f })
		{
			var glow = new MeshInstance3D
			{
				Name = side < 0 ? "PortExhaustGlow" : "StarboardExhaustGlow",
				Position = new Vector3(
					center.X + side * bounds.Size.X * 0.42f, center.Y,
					bounds.Position.Z + bounds.Size.Z * 0.18f),
				Mesh = new SphereMesh
				{
					Radius = subtle ? 0.065f : 0.11f,
					Height = subtle ? 0.13f : 0.22f,
				},
				MaterialOverride = new StandardMaterial3D
				{
					ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
					AlbedoColor = subtle ? new Color(2.2f, 0.85f, 0.12f) : new Color(0.4f, 1.8f, 3f),
				},
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkWorld(glow);
			AddChild(glow);
			_exhaust.Add(glow);
		}
		SetCharge(0);
	}

	public void SetCharge(float seconds)
	{
		var charge = Mathf.Clamp(seconds / (float)RevealSeconds, 0, 1);
		for (var i = 0; i < _strokes.Count; i++)
			_strokes[i].Visible = seconds >= (i + 1) * RevealSeconds / _strokes.Count;
		foreach (var glow in _exhaust)
			glow.Scale = new Vector3(1, 1, 1.8f) * (0.2f + charge * 1.1f);
	}

	private static ArrayMesh CreateStroke(Vector3[] points)
	{
		var vertices = new List<Vector3>();
		for (var i = 0; i < points.Length - 1; i++)
		{
			for (var side = 0; side < 6; side++)
			{
				Vector3 At(int segment, int edge)
				{
					var angle = Mathf.Tau * edge / 6f;
					return points[segment] + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 0.012f;
				}
				var a = At(i, side);
				var b = At(i, side + 1);
				var c = At(i + 1, side);
				var d = At(i + 1, side + 1);
				vertices.AddRange([a, c, b, b, c, d]);
			}
		}
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
