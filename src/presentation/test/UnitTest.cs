using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Loadouts.Defenses;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Presentation.Test;

public partial class UnitTest : Node3D
{
	public override void _Ready()
	{
		var mainCamera = GetNode<Controller>("Camera3D");
		GD.Print($"UnitTest: Camera position = {mainCamera.GlobalPosition}");
		GD.Print($"UnitTest: Camera cull mask = {mainCamera.CullMask}");
		
		// Test: Set up BattleCamera with PlayerAft pose to follow player
		GD.Print("UnitTest: Setting up BattleCamera PlayerAft pose...");
		var playerState = new State
		{
			Id = "player-fighter",
			Position = new Coord(0, 0, 0),
			Fore = Coord.Forward,
			Dorsal = Coord.Up,
			Starboard = Coord.Cross(Coord.Up, Coord.Forward),
			Type = EType.Fighter,
			Initiative = 0,
			Loadout = new ShipLoadout(100, new FaceShieldPoints(), new List<InstalledAbility>(), new FaceShieldPoints()),
			Maneuverability = new ManeuverabilitySpec(
				2, 2, 
				new System.Collections.Generic.Dictionary<ESpatialOrientation, int>(),
				new System.Collections.Generic.Dictionary<EHeadingTurn, int>(),
				new System.Collections.Generic.Dictionary<ERollDirection, int>()),
			HullPoints = 100,
		};
		
		var openingPose = BattleCameraPoses.PlayerAft(playerState);
		mainCamera.SetFocus(
			openingPose.Pivot,
			openingPose.Distance,
			openingPose.Yaw,
			openingPose.Pitch);
		GD.Print($"UnitTest: BattleCamera set to PlayerAft - Pivot={openingPose.Pivot}, Distance={openingPose.Distance}, Yaw={openingPose.Yaw}, Pitch={openingPose.Pitch}");
		
		// Test: Add SpaceBackdrop
		GD.Print("UnitTest: Adding SpaceBackdrop...");
		var grid = new GrimSpace.Math.Grid.Grid(64, 64, 64);
		var backdrop = new SpaceBackdrop();
		backdrop.Build(grid, 99999);
		AddChild(backdrop);
		MoveChild(backdrop, 0);
		GD.Print("UnitTest: SpaceBackdrop added");
		
		// Test: Add GridView
		GD.Print("UnitTest: Adding GridView...");
		var cellVolumeMeshes = new CellVolumeMeshStore();
		var gridView = new GridView();
		gridView.Build(mainCamera, cellVolumeMeshes);
		AddChild(gridView);
		GD.Print("UnitTest: GridView added");
		
		// Test: Add BoardAsteroidView (hazards)
		GD.Print("UnitTest: Adding BoardAsteroidView...");
		var hazardsRoot = new Node3D { Name = "WorldHazards" };
		AddChild(hazardsRoot);
		var asteroidView = new BoardAsteroidView();
		
		// Create some test asteroids
		var asteroids = new List<Asteroid>();
		for (int i = 0; i < 50; i++)
		{
			var origin = new Coord(
				new System.Random().Next(0, 64),
				new System.Random().Next(0, 64),
				new System.Random().Next(0, 64));
			var cells = new HashSet<Coord>();
			cells.Add(origin);
			asteroids.Add(Asteroid.Create(
				GrimSpace.Core.Ids.TypedIdGenerator.NextId("asteroid"),
				origin,
				new GrimSpace.Math.Grid.Grid(64, 64, 64),
				cells));
		}
		
		var asteroidViewInstance = new BoardAsteroidView();
		asteroidViewInstance.Build(mainCamera, asteroids);
		hazardsRoot.AddChild(asteroidViewInstance);
		GD.Print("UnitTest: BoardAsteroidView added");
		
		// Create multiple units: player + enemies
		var unitConfigs = new[]
		{
			new { Id = "player-fighter", Position = new Coord(0, 0, 0), Team = "Player", Color = new Godot.Color(0.25f, 0.85f, 0.35f) },
			new { Id = "enemy-carrier", Position = new Coord(10, 0, 10), Team = "Enemy", Color = new Godot.Color(0.9f, 0.25f, 0.2f) },
			new { Id = "enemy-gooper", Position = new Coord(-10, 0, -10), Team = "Enemy", Color = new Godot.Color(0.9f, 0.25f, 0.2f) },
			new { Id = "enemy-miner", Position = new Coord(5, 0, -5), Team = "Enemy", Color = new Godot.Color(0.9f, 0.25f, 0.2f) },
		};

		foreach (var u in unitConfigs)
		{
			var state = new State
			{
				Id = u.Id,
				Position = u.Position,
				Fore = Coord.Forward,
				Dorsal = Coord.Up,
				Starboard = Coord.Cross(Coord.Up, Coord.Forward),
				Type = u.Id.Contains("carrier") ? EType.Carrier : 
					   u.Id.Contains("gooper") ? EType.IndustrialGooper :
					   u.Id.Contains("miner") ? EType.RepurposedMiner : EType.Fighter,
				Initiative = 0,
				Loadout = new ShipLoadout(100, new FaceShieldPoints(), new List<InstalledAbility>(), new FaceShieldPoints()),
				Maneuverability = new ManeuverabilitySpec(
					2, 2, 
					new System.Collections.Generic.Dictionary<ESpatialOrientation, int>(),
					new System.Collections.Generic.Dictionary<EHeadingTurn, int>(),
					new System.Collections.Generic.Dictionary<ERollDirection, int>()),
				HullPoints = 100,
			};

			var unitView = new UnitView();
			var unitRootNode = GetNode<Node3D>("UnitRoot");
			unitRootNode.AddChild(unitView);
			unitView.Bind(state, u.Color);
			
			GD.Print($"UnitTest: {u.Id} at {state.Position} - Visible={unitView.Visible}, VisualState={unitView.VisualState}");
		}
		
		GD.Print($"UnitTest: Total units created = 4");
		
		// Test: Add TurnReplayPlayer (replay system)
		GD.Print("UnitTest: Adding TurnReplayPlayer...");
		var unitRootNode2 = GetNode<Node3D>("UnitRoot");
		var battleViewNode = unitRootNode2.GetNodeOrNull<BattleView>("BattleView");
		if (battleViewNode == null)
		{
			var bv = new BattleView { Name = "BattleView" };
			unitRootNode2.AddChild(bv);
			battleViewNode = bv;
		}
		
		var turnReplayPlayer = new TurnReplayPlayer { Name = "TurnReplayPlayer" };
		turnReplayPlayer.Configure(
			battleViewNode.UnitViews,
			id => unitConfigs.First(u => u.Id == id).Color,
			(state, color) => 
			{
				var view = new UnitView();
				GetNode<Node3D>("UnitRoot").AddChild(view);
				view.Bind(state, color);
			},
			id => 
			{
				if (battleViewNode.UnitViews.TryGetValue(id, out var view))
					view.QueueFree();
			},
			states => battleViewNode.ApplyUnitStates(states, id => unitConfigs.First(u => u.Id == id).Color),
			state => { }, // stateChanged
			_ => { }, // showGoop
			_ => { }); // removeGoop
		unitRootNode2.AddChild(turnReplayPlayer);
		GD.Print("UnitTest: TurnReplayPlayer configured");
		
		// Test: Simulate a simple replay
		GD.Print("UnitTest: Simulating TurnReplayPlayer reset...");
		var startStates = unitConfigs.ToDictionary(
			u => u.Id, 
			u => CreateUnitState(u.Id, u.Position, 
				u.Id.Contains("carrier") ? EType.Carrier :
				u.Id.Contains("gooper") ? EType.IndustrialGooper :
				u.Id.Contains("miner") ? EType.RepurposedMiner : EType.Fighter));
		
		var endStates = new Dictionary<string, State>(startStates);
		// Move player forward a bit
		var playerEndState = startStates["player-fighter"];
		playerEndState = new State
		{
			Id = playerEndState.Id,
			Position = new Coord(3, 0, 0),
			Fore = playerEndState.Fore,
			Dorsal = playerEndState.Dorsal,
			Starboard = playerEndState.Starboard,
			Type = playerEndState.Type,
			Initiative = playerEndState.Initiative,
			Loadout = playerEndState.Loadout,
			Maneuverability = playerEndState.Maneuverability,
			HullPoints = playerEndState.HullPoints,
		};
		endStates["player-fighter"] = playerEndState;
		
		turnReplayPlayer.ResetToLive(startStates, endStates, interest => GD.Print($"UnitTest: CameraInterest - {interest.Points.Count} points"));
		GD.Print("UnitTest: TurnReplayPlayer.ResetToLive complete");
		
		GD.Print("UnitTest: Replay test complete");
	}
	
	private static State CreateUnitState(string id, Coord position, EType type)
	{
		return new State
		{
			Id = id,
			Position = position,
			Fore = Coord.Forward,
			Dorsal = Coord.Up,
			Starboard = Coord.Cross(Coord.Up, Coord.Forward),
			Type = type,
			Initiative = 0,
			Loadout = new ShipLoadout(100, new FaceShieldPoints(), new List<InstalledAbility>(), new FaceShieldPoints()),
			Maneuverability = new ManeuverabilitySpec(
				2, 2, 
				new System.Collections.Generic.Dictionary<ESpatialOrientation, int>(),
				new System.Collections.Generic.Dictionary<EHeadingTurn, int>(),
				new System.Collections.Generic.Dictionary<ERollDirection, int>()),
			HullPoints = 100,
		};
	}
}