using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GrimSpace.Tests.Encounter;

[BattleTestSuite]
public sealed class GravityWellEncounterTests
{
	private static readonly Coord Center = new(6, 6, 6);
	private static readonly Coord Right = new(1, 0, 0);
	private static readonly BodyFrame RotatedFrame = new(Center, Right, Coord.Up, -Coord.Forward);

	[Fact]
	public void FixedEncounterAndWorldSaveRoundTripPreserveRotatedWell()
	{
		var encounter = CreateEncounter(CreateSpawn());
		var savedEncounter = SaveDtoMapper.CaptureBattleEncounter(encounter);
		var options = new PersistenceRegistry().Options;
		var encounterJson = JsonSerializer.SerializeToElement(savedEncounter, options);
		var restoredEncounter = SaveDtoMapper.RestoreBattleEncounter(
			ReflectionJson.Read<BattleEncounterSaveDto>(encounterJson, options));
		using var battle = BattleOrchestrator.FromEncounter(restoredEncounter, gridSize: 16);
		var world = battle.Engine.World;
		var well = Assert.Single(world.Hazards.OfType<GravityWell>());
		Assert.Equal(RotatedFrame, well.Frame);
		Assert.True(well.Cells.SetEquals(Coord.OffsetsInCube(1).Select(offset => Center + offset)));
		Assert.True(world.IsCellBlocked(Center));
		Assert.All(well.AssistCells, cell => Assert.False(world.IsCellBlocked(cell)));
		Assert.True(world.IsTranslationAssisted(Center - Coord.Forward, Right));
		Assert.False(world.IsTranslationAssisted(Center - Coord.Forward, -Right));
		Assert.Same(well, world.Fork().NonUnits[well.Id]);

		var registry = new PersistenceRegistry();
		var dto = SaveDtoMapper.CaptureBattleWorld(world, registry);
		var json = JsonSerializer.SerializeToElement(dto, registry.Options);
		var restored = SaveDtoMapper.RestoreBattleWorld(
			ReflectionJson.Read<BattleWorldSaveDto>(json, registry.Options), registry);
		var restoredWell = Assert.Single(restored.Hazards.OfType<GravityWell>());
		Assert.Equal(well.Frame, restoredWell.Frame);
		Assert.True(well.Cells.SetEquals(restoredWell.Cells));
		foreach (var cell in well.AssistCells)
			Assert.True(well.AssistedDirectionsByCell[cell].SetEquals(restoredWell.AssistedDirectionsByCell[cell]));
		Assert.True(restored.IsCellBlocked(Center));
		Assert.True(restored.IsTranslationAssisted(Center - Coord.Forward, Right));
		Assert.Contains(Center, (IReadOnlySet<Coord>)restored.AbilityBlockingCells);
	}

	[Fact]
	public void OlderEncounterWithoutHazardFramesStillLoads()
	{
		var plume = new BattleHazardSpawn
		{
			Kind = EBattleHazardKind.CryoPlume,
			Origin = Center,
			Cells = new HashSet<Coord> { Center },
		};
		var options = new PersistenceRegistry().Options;
		var saved = SaveDtoMapper.CaptureBattleEncounter(CreateEncounter(plume));
		var json = JsonSerializer.SerializeToNode(saved, options)!.AsObject();
		json["hazards"]!.AsArray()[0]!.AsObject().Remove("frame");
		var restored = SaveDtoMapper.RestoreBattleEncounter(
			JsonSerializer.Deserialize<BattleEncounterSaveDto>(json, options)!);

		using var battle = BattleOrchestrator.FromEncounter(restored, gridSize: 16);
		Assert.NotNull(battle.Engine.World.CryoPlumeAt(Center));
	}

	[Fact]
	public void EncounterCoreBlocksShotsButSurroundingCellsDoNot()
	{
		using var battle = BattleOrchestrator.FromEncounter(CreateEncounter(CreateSpawn()), gridSize: 16);
		var world = battle.Engine.World;
		var action = new LightningCannonAction("player");
		var throughCore = LightningCannonDef.Instance.AffectedCells(action, world);
		Assert.DoesNotContain(Center, throughCore);
		Assert.DoesNotContain(Center + Coord.Forward, throughCore);

		world.StateOf("player").Position += Right;
		var throughAssist = LightningCannonDef.Instance.AffectedCells(action, world);
		Assert.Contains(Center + Right, throughAssist);
		Assert.Contains(Center + Right + Coord.Forward, throughAssist);
	}

	[Fact]
	public void InvalidWellCellsFrameAndBoundsAreRejected()
	{
		var grid = BattleTestFixture.Grid(16);
		var clippedCenter = Coord.Zero;
		Assert.Throws<ArgumentOutOfRangeException>(() => new BattleHazardSpawn
		{
			Kind = EBattleHazardKind.GravityWell,
			Origin = clippedCenter,
			Frame = BodyFrame.WorldAligned(clippedCenter),
			Cells = Coord.OffsetsInCube(1).ToHashSet(),
		}.CreateNonUnit(grid));
		Assert.Throws<ArgumentException>(() => new BattleHazardSpawn
		{
			Kind = EBattleHazardKind.GravityWell,
			Origin = Center,
			Frame = RotatedFrame,
			Cells = new HashSet<Coord> { Center },
		}.CreateNonUnit(grid));
		Assert.Throws<InvalidOperationException>(() => new BattleHazardSpawn
		{
			Kind = EBattleHazardKind.GravityWell,
			Origin = Center,
			Cells = CreateSpawn().Cells,
		}.CreateNonUnit(grid));
	}

	[Fact]
	public void InvalidSavedFootprintAndMissingCoreBlockerAreRejected()
	{
		using var battle = BattleOrchestrator.FromEncounter(CreateEncounter(CreateSpawn()), gridSize: 16);
		var registry = new PersistenceRegistry();
		var dto = SaveDtoMapper.CaptureBattleWorld(battle.Engine.World, registry);
		var hazard = Assert.Single(dto.Hazards);
		Assert.Throws<ArgumentException>(() => SaveDtoMapper.RestoreBattleWorld(
			dto with { Hazards = [hazard with { Cells = [Center] }] }, registry));
		Assert.Throws<InvalidDataException>(() => SaveDtoMapper.RestoreBattleWorld(
			dto with { BlockedCells = [] }, registry));
	}

	private static BattleHazardSpawn CreateSpawn() => new()
	{
		Kind = EBattleHazardKind.GravityWell,
		Origin = Center,
		Frame = RotatedFrame,
		Cells = Coord.OffsetsInCube(1).Select(offset => Center + offset).ToHashSet(),
	};

	private static BattleEncounter CreateEncounter(params BattleHazardSpawn[] hazards) => new()
	{
		Id = "gravitywell-encounter",
		Seed = 1,
		Objective = EObjective.EliminateOpponents,
		Spawns =
		[
			BattleSpawnTestKit.Create("player", EType.Fighter, ETeam.Player,
				Center - Coord.Forward * 3, new UserExecutionAgent()),
			BattleSpawnTestKit.Create("enemy", EType.Fighter, ETeam.Enemy,
				Center + Coord.Forward * 3, new AiController()),
		],
		WorldHazards = hazards,
	};
}
