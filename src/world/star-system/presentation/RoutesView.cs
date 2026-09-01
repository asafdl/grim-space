using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class RoutesView : Node3D
{
	private const float YOffset = 0.003f;

	private static readonly Color CenterlineColor = new(0.28f, 0.40f, 0.48f, 0.14f);

	public void Build(StarMap world)
	{
		foreach (var child in GetChildren().ToArray())
		{
			RemoveChild(child);
			child.Free();
		}

		foreach (var route in world.RoutesById.Values.OrderBy(route => route.Id, StringComparer.Ordinal))
			AddChild(BuildRoute(route, world.Width, world.Height));
	}

	private static MeshInstance3D BuildRoute(SpaceRoute route, int width, int height)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceSetColor(CenterlineColor);

		for (var i = 1; i < route.Centerline.Count; i++)
		{
			mesh.SurfaceAddVertex(ToRouteWorld(route.Centerline[i - 1], width, height));
			mesh.SurfaceAddVertex(ToRouteWorld(route.Centerline[i], width, height));
		}

		mesh.SurfaceEnd();
		return new MeshInstance3D
		{
			Name = $"Route_{route.Id}",
			Mesh = mesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				VertexColorUseAsAlbedo = true,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			},
		};
	}

	private static Vector3 ToRouteWorld(Coord point, int mapWidth, int mapHeight) =>
		MapMapping.ToWorld(point, mapWidth, mapHeight) + Vector3.Up * YOffset;
}
