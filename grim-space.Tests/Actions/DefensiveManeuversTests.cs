using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
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
	public void Bootstrap_AppendsStickyStartSyncEntry()
	{
		var (player, battle) = PlayerWithDefensiveManeuversBattle();
		var entry = Assert.Single(battle.Engine.World.ActivationRoundLists.EntriesFor(player.State.Id));
		Assert.Equal(EUnitActivationPhase.Start, entry.RunAt);
		Assert.Null(entry.RoundsRemaining);
		Assert.True(entry.Sticky);
		Assert.IsType<SyncDefensiveManeuversMpUsedAction>(entry.Action);
	}

	[Fact]
	public void StartPass_SyncsMpUsedFromPool()
	{
		var (player, battle) = PlayerWithDefensiveManeuversBattle();
		player.State.ManeuverPoints = 1;

		BattleTestApply.CommitActivationRoundPasses(
			battle,
			player.State.Id,
			EUnitActivationPhase.Start,
			1);

		Assert.Equal(2, player.State.DefensiveManeuversMpUsed);
	}

	[Fact]
	public void MpChangeEffect_UpdatesMpUsedWhenActive()
	{
		var (player, battle) = PlayerWithDefensiveManeuversBattle();
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var effect = new MpChangeEffect(-2);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(2, player.State.DefensiveManeuversMpUsed);

		effect.Undo(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(0, player.State.DefensiveManeuversMpUsed);
	}

	[Fact]
	public void SyncEffect_UndoRestoresPreviousMpUsed()
	{
		var (player, battle) = PlayerWithDefensiveManeuversBattle();
		player.State.DefensiveManeuversMpUsed = 1;
		var effect = new SyncDefensiveManeuversMpUsedFromPoolEffect();
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);
		effect.Undo(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(1, player.State.DefensiveManeuversMpUsed);
	}

	[Fact]
	public void StateClone_PreservesDefensiveManeuversFields()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.DefensiveManeuversActive = true;
		state.DefensiveManeuversMpUsed = 0;
		state.DefensiveManeuversRollIndex = 4;

		var clone = state.Clone();

		Assert.True(clone.DefensiveManeuversActive);
		Assert.Equal(0, clone.DefensiveManeuversMpUsed);
		Assert.Equal(4, clone.DefensiveManeuversRollIndex);
	}

	[Fact]
	public void BattleUnitStateDto_PreservesDefensiveManeuversFields()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.DefensiveManeuversActive = true;
		state.DefensiveManeuversMpUsed = 2;
		state.DefensiveManeuversRollIndex = 7;

		var dto = GrimSpace.Run.Persistence.SaveDtoMapper.CaptureBattleUnitState(state);
		var restored = GrimSpace.Run.Persistence.SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.Equal(2, restored.DefensiveManeuversMpUsed);
		Assert.Equal(7, restored.DefensiveManeuversRollIndex);
	}

	private static (Unit Player, BattleOrchestrator Battle) PlayerWithDefensiveManeuversBattle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
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
