using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class DefensiveManeuversTests
{
	[Fact]
	public void EndOfPhase_ReplacesLatestTurnManeuverPointSpend()
	{
		var (player, battle) = PlayerWithDefensiveManeuversBattle();

		battle.Engine.Commit(new EndOfPhaseAction(player.State.Id));
		Assert.Equal(0, player.State.DefensiveManeuversMpSpentLastTurn);

		player.State.ManeuverPoints = 1;
		battle.Engine.Commit(new EndOfPhaseAction(player.State.Id));

		Assert.Equal(2, player.State.DefensiveManeuversMpSpentLastTurn);
	}

	[Fact]
	public void EndOfPhase_WithoutSkillDoesNotRecordCondition()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));

		battle.Engine.Commit(new EndOfPhaseAction(player.State.Id));

		Assert.Null(player.State.DefensiveManeuversMpSpentLastTurn);
	}

	[Fact]
	public void RecordEffect_UndoRestoresPreviousTurn()
	{
		var (player, battle) = PlayerWithDefensiveManeuversBattle();
		player.State.DefensiveManeuversMpSpentLastTurn = 1;
		var effect = new RecordDefensiveManeuversTurnEffect();
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);
		effect.Undo(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(1, player.State.DefensiveManeuversMpSpentLastTurn);
	}

	[Fact]
	public void StateClone_PreservesDefensiveManeuversConditionAndRollSequence()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.DefensiveManeuversMpSpentLastTurn = 0;
		state.DefensiveManeuversRollIndex = 4;

		var clone = state.Clone();

		Assert.Equal(0, clone.DefensiveManeuversMpSpentLastTurn);
		Assert.Equal(4, clone.DefensiveManeuversRollIndex);
	}

	[Fact]
	public void BattleUnitStateDto_PreservesDefensiveManeuversConditionAndRollSequence()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.DefensiveManeuversMpSpentLastTurn = 2;
		state.DefensiveManeuversRollIndex = 7;

		var dto = GrimSpace.Run.Persistence.SaveDtoMapper.CaptureBattleUnitState(state);
		var restored = GrimSpace.Run.Persistence.SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.Equal(2, restored.DefensiveManeuversMpSpentLastTurn);
		Assert.Equal(7, restored.DefensiveManeuversRollIndex);
	}

	private static (Unit Player, BattleOrchestrator Battle) PlayerWithDefensiveManeuversBattle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = new PilotBattleSnapshot(
				"pilot",
				[EPilotSkillId.DefensiveManeuvers]),
		};
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			pilotsByActorId: pilots);
		return (player, battle);
	}
}
