using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Presentation;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Presentation;

public sealed class UnitsViewSyncTests
{
	[Fact(Skip = "Requires Godot scene tree runtime; run from the Godot editor test harness.")]
	public void UnitsView_AddRemoveOnSync()
	{
		var map = StarMap.CreateDevDefault(42);
		var view = new UnitsView();
		view.Build(map);
		Assert.Equal(map.UnitRegistry.All.Count(), view.GetChildCount());

		var addedId = "view-test-pirate";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			addedId,
			new Coord(100, 0, 200),
			EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));
		view.Sync(map, 0f);
		Assert.Equal(map.UnitRegistry.All.Count(), view.GetChildCount());
		Assert.NotNull(view.GetNodeOrNull($"Unit_{addedId}"));

		map.UnitRegistry.Remove(addedId);
		view.Sync(map, 0f);
		Assert.Equal(map.UnitRegistry.All.Count(), view.GetChildCount());
		Assert.Null(view.GetNodeOrNull($"Unit_{addedId}"));
	}
}
