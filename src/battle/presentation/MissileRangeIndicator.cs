using Godot;
using GrimSpace.Domain.Grid;
using GridView = GrimSpace.Battle.Grid.View;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Soft L1-range shell (octahedron) centered on the firing ship.
/// </summary>
public partial class MissileRangeIndicator : Node3D
{
	private MeshInstance3D? _mesh;

	public void SetActive(Coord? shipCell, int rangeTiles)
	{
		if (shipCell is null || rangeTiles <= 0)
		{
			if (_mesh is not null)
				_mesh.Visible = false;

			return;
		}

		EnsureMesh(rangeTiles);
		Position = GridView.ToWorld(shipCell.Value);
		_mesh!.Visible = true;
	}

	private void EnsureMesh(int rangeTiles)
	{
		if (_mesh is not null)
			return;

		var radius = rangeTiles * GridView.CellSize;
		_mesh = new MeshInstance3D
		{
			Mesh = CreateManhattanShell(radius),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.35f, 0.8f, 1f, 0.08f),
				Roughness = 1f,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
				DisableReceiveShadows = true,
			},
		};
		AddChild(_mesh);
	}

	private static ArrayMesh CreateManhattanShell(float radius)
	{
		var vertices = new Vector3[]
		{
			new(radius, 0f, 0f),
			new(-radius, 0f, 0f),
			new(0f, radius, 0f),
			new(0f, -radius, 0f),
			new(0f, 0f, radius),
			new(0f, 0f, -radius),
		};

		var indices = new int[]
		{
			0, 4, 2,  0, 2, 5,  0, 5, 3,  0, 3, 4,
			1, 2, 4,  1, 5, 2,  1, 3, 5,  1, 4, 3,
		};

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}
}
