using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation.Map;

namespace GrimSpace.World.StarSystem.Presentation.Picking;

public static class MapInteractivePick
{
	public readonly record struct Context(
		Camera3D Camera,
		Vector2 ScreenPos,
		Coord? GridPoint,
		StarSystemOrchestrator Orchestrator,
		float TickFraction,
		UnitsView Units,
		MapView View,
		NavigationLandmarksView Landmarks,
		WreckageView Wreckage,
		Func<string, bool> IsFleetVisible);

	public static MapInteractiveTarget Resolve(Context context)
	{
		var grid = context.GridPoint;

		if (context.Units.PickAtScreen(context) is { } unit)
			return new MapInteractiveTarget(MapInteractiveTargetKind.Unit, unit, null, null, null, null, null);

		if (context.Wreckage.PickAtScreen(context) is { } wreckContractId)
			return new MapInteractiveTarget(MapInteractiveTargetKind.Wreck, null, wreckContractId, null, null, null, null);

		if (context.Landmarks.PickAtScreen(context) is { } landmarkId)
			return new MapInteractiveTarget(
				MapInteractiveTargetKind.Landmark, null, null, landmarkId, null, null, grid);

		if (context.View.PickDockAtScreen(context) is { } dock)
			return new MapInteractiveTarget(MapInteractiveTargetKind.Dock, null, null, null, dock, null, grid);

		if (context.View.PickPoiAtScreen(context) is { } poiId)
			return new MapInteractiveTarget(MapInteractiveTargetKind.Poi, null, null, null, null, poiId, grid);

		if (grid is not { } moveGrid)
			return new MapInteractiveTarget(MapInteractiveTargetKind.None, null, null, null, null, null, null);

		if (context.Units.IsPlayerFleetAtGrid(
				context.Camera,
				context.ScreenPos,
				moveGrid,
				context.Orchestrator,
				context.TickFraction))
			return MapInteractiveTarget.SelfClick();

		return MapInteractiveTarget.Move(moveGrid);
	}
}
