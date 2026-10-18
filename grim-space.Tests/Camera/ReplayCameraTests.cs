using GrimSpace.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;
using Godot;
using Xunit;

namespace GrimSpace.Tests.Camera;

[BattleTestSuite]
public sealed class ReplayCameraTests
{
	private static State CreateTestState(string id, Coord position, Coord fore, Coord dorsal)
	{
		var emptyShields = new FaceShieldPoints();
		var emptyAbilities = new List<InstalledAbility>();
		
		var state = new State
		{
			Id = id,
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			Starboard = Coord.Cross(dorsal, fore),
			Type = EType.Fighter,
			Initiative = 0,
			Loadout = new ShipLoadout(100, emptyShields, emptyAbilities, emptyShields),
			Maneuverability = new ManeuverabilitySpec(
				2, 2, 
				new Dictionary<ESpatialOrientation, int>(),
				new Dictionary<EHeadingTurn, int>(),
				new Dictionary<ERollDirection, int>()),
		};
		return state;
	}

	[Fact]
	public void UnitViewHullIsOnWorldRenderLayer()
	{
		var state = CreateTestState("test-unit", new Coord(10, 32, 32), Coord.Forward, Coord.Up);
		
		var view = new UnitView();
		view.Bind(state, new Godot.Color(1,1,1));
		
		// Find the hull child (MeshInstance3D) and verify it's on World render layer
		var hullChild = view.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>()
			.FirstOrDefault(m => m.Name.ToString().Contains("Hull") || m.Name.ToString().Contains("Fighter") || m.Name.ToString().Contains("Carrier") || m.Name.ToString().Contains("Gunship") || m.Name.ToString().Contains("Gooper") || m.Name.ToString().Contains("Miner") || m.Name.ToString().Contains("Void"));
		
		Assert.NotNull(hullChild);
		Assert.True(hullChild.Layers == PresentationLayers.World, 
			$"Hull should be on World layer (1), but is on layer {hullChild.Layers}");
	}

	[Fact]
	public void UnitViewHullIsVisibleByDefault()
	{
		var state = CreateTestState("test-unit", new Coord(10, 32, 32), Coord.Forward, Coord.Up);
		
		var view = new UnitView();
		view.Bind(state, new Godot.Color(1,1,1));
		
		// Find hull and verify it's visible
		var hullChild = view.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>()
			.FirstOrDefault(m => m.Name.ToString().Contains("Hull") || m.Name.ToString().Contains("Fighter") || m.Name.ToString().Contains("Carrier") || m.Name.ToString().Contains("Gunship") || m.Name.ToString().Contains("Gooper") || m.Name.ToString().Contains("Miner") || m.Name.ToString().Contains("Void"));
		
		Assert.NotNull(hullChild);
		Assert.True(hullChild.Visible, "Hull should be visible after Bind");
		Assert.True(view.Visible, "UnitView should be visible");
	}

	[Fact]
	public void CameraCullMaskIncludesWorldLayer()
	{
		var camera = new Controller();
		
		// Camera should render World layer (1) and Ux layer (2)
		var expectedMask = (uint)(PresentationLayers.World | PresentationLayers.Ux);
		Assert.Equal(expectedMask, camera.CullMask);
	}

	[Fact]
	public void ReplayDirectorBeginPlaybackSetsPlaybackMode()
	{
		var camera = new Controller();
		var cameraDirector = new BattleCameraDirector(camera);
		
		// Initially in Manual mode
		Assert.False(cameraDirector.NeedsTick);
		
		cameraDirector.BeginPlayback();
		
		// After BeginPlayback, should be in Playback mode
		Assert.True(cameraDirector.NeedsTick, "CameraDirector should need ticks in Playback mode");
	}

	[Fact]
	public void BattleCameraDirectorReportsInterestDuringPlayback()
	{
		var camera = new Controller();
		var cameraDirector = new BattleCameraDirector(camera);
		
		cameraDirector.BeginPlayback();
		
		var interest = new CameraInterest(
			new[] { new Vector3(0, 0, 0) },
			CameraImportance.Combat);
		
		// Should not throw and should process the interest
		cameraDirector.ReportInterest(interest);
		
		// In Playback mode with no automation, should start moving toward target
		Assert.True(cameraDirector.NeedsTick);
	}
}