using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// 3D render layers for separating world lighting from planning/replay overlays.
/// </summary>
public static class PresentationLayers
{
	public const uint World = 1;
	public const uint Ux = 2;

	public static void MarkWorld(VisualInstance3D node) => node.Layers = World;

	public static void MarkUx(VisualInstance3D node) => node.Layers = Ux;
}
