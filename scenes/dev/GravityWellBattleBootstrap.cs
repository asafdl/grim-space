using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Scene;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Scenes.Dev;

public partial class GravityWellBattleBootstrap : Node
{
	public override void _Ready()
	{
		if (Session.Instance.Run is null)
			Session.Instance.StartNewRun();

		var first = new Coord(31, 32, 32);
		var second = new Coord(35, 32, 34);
		var right = new Coord(1, 0, 0);
		var encounter = new BattleEncounter
		{
			Id = "engagement-dev-gravitywell",
			Seed = 42,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("fighter-dev-gravitywell", EType.Fighter),
					Team = ETeam.Player,
					Faction = EFaction.Player,
					Position = new Coord(31, 32, 28),
					ExecutionAgent = new UserExecutionAgent(),
				},
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("fighter-enemy-gravitywell", EType.Fighter),
					Team = ETeam.Enemy,
					Faction = EFaction.TheOptimality,
					Position = new Coord(31, 32, 40),
					Fore = -Coord.Forward,
					ExecutionAgent = new AiController(),
				},
			],
			WorldHazards =
			[
				Well(first, BodyFrame.WorldAligned(first)),
				Well(second, new BodyFrame(second, right, Coord.Up, -Coord.Forward)),
			],
		};
		var scene = GD.Load<PackedScene>("res://scenes/battle.tscn")
			?? throw new InvalidOperationException("Could not load battle scene.");
		var battle = scene.Instantiate<BattleController>();
		battle.DevEncounter = encounter;
		AddChild(battle);
	}

	private static BattleHazardSpawn Well(Coord center, BodyFrame frame) => new()
	{
		Kind = EBattleHazardKind.GravityWell,
		Origin = center,
		Frame = frame,
		Cells = Coord.OffsetsInCube(1).Select(offset => center + offset).ToHashSet(),
	};
}
