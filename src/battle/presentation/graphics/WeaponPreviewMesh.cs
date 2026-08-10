using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Shared ring-extrusion mesh used by railgun/flak volume previews.</summary>
internal static class WeaponPreviewMesh
{
	internal readonly record struct Section(
		float DistanceInCells,
		float RadiusInCells,
		float Alpha);

	public static ArrayMesh CreatePlume(IReadOnlyList<Section> sections, int ringSides)
	{
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var colors = new List<Color>();
		var indices = new List<int>();

		for (var sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
		{
			var section = sections[sectionIndex];

			for (var side = 0; side < ringSides; side++)
			{
				var angle = Mathf.Tau * side / ringSides;
				var x = Mathf.Cos(angle);
				var y = Mathf.Sin(angle);

				var wobble =
					1f
					+ 0.07f * Mathf.Sin(angle * 3f + sectionIndex * 0.8f)
					+ 0.035f * Mathf.Sin(angle * 5f - sectionIndex * 0.4f);

				var radius =
					section.RadiusInCells
					* WorldMapping.CellSize
					* wobble;

				vertices.Add(new Vector3(
					x * radius,
					y * radius,
					section.DistanceInCells * WorldMapping.CellSize));

				normals.Add(new Vector3(x, y, 0f).Normalized());

				var alphaVariation =
					0.9f + 0.1f * Mathf.Sin(angle * 4f + sectionIndex);

				colors.Add(new Color(
					1f,
					1f,
					1f,
					section.Alpha * alphaVariation));
			}
		}

		for (var section = 0; section < sections.Count - 1; section++)
		{
			for (var side = 0; side < ringSides; side++)
			{
				var nextSide = (side + 1) % ringSides;

				var a = section * ringSides + side;
				var b = section * ringSides + nextSide;
				var c = (section + 1) * ringSides + side;
				var d = (section + 1) * ringSides + nextSide;

				indices.Add(a);
				indices.Add(c);
				indices.Add(b);

				indices.Add(b);
				indices.Add(c);
				indices.Add(d);
			}
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
		arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
