using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ShieldDynamoTests
{
	[Fact]
	public void Bootstrap_AppendsStickyStartExpireEntry()
	{
		var (player, battle) = PlayerWithShieldDynamoBattle();
		var entry = Assert.Single(
			battle.Engine.World.ActivationRoundLists.EntriesFor(player.State.Id),
			e => e.Action is ExpireShieldDynamoGrantsAction);
		Assert.Equal(EUnitActivationPhase.Start, entry.RunAt);
		Assert.Null(entry.RoundsRemaining);
		Assert.True(entry.Sticky);
	}

	[Fact]
	public void ApSpend_GrantsShieldsOnEveryFace()
	{
		var (player, battle) = PlayerWithShieldDynamoBattle();
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			player.State.CurrentShieldPoints[face] = 10;
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var effect = new ApChangeEffect(-2);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(20, player.State.CurrentShieldPoints[face]);
		Assert.Equal(10, player.State.ShieldDynamoGrantedPerFace);
	}

	[Fact]
	public void ApSpend_CanExceedBaseShields()
	{
		var (player, battle) = PlayerWithShieldDynamoBattle();
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			player.State.CurrentShieldPoints[face] = player.State.Loadout.BaseShieldPoints[face];
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		new ApChangeEffect(-1).Apply(battle.Engine.World, runtime, player.State.Id);

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			Assert.True(
				player.State.CurrentShieldPoints[face] > player.State.Loadout.BaseShieldPoints[face]);
		}
	}

	[Fact]
	public void StartPass_ExpiresGrantedShields()
	{
		var (player, battle) = PlayerWithShieldDynamoBattle();
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			player.State.CurrentShieldPoints[face] = 30;
		player.State.ShieldDynamoGrantedPerFace = 10;

		BattleTestApply.CommitActivationRoundPasses(
			battle,
			player.State.Id,
			EUnitActivationPhase.Start,
			1);

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(20, player.State.CurrentShieldPoints[face]);
		Assert.Equal(0, player.State.ShieldDynamoGrantedPerFace);
	}

	[Fact]
	public void ApChangeEffect_UndoRestoresShields()
	{
		var (player, battle) = PlayerWithShieldDynamoBattle();
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			player.State.CurrentShieldPoints[face] = 10;
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var effect = new ApChangeEffect(-1);

		effect.Apply(battle.Engine.World, runtime, player.State.Id);
		effect.Undo(battle.Engine.World, runtime, player.State.Id);

		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(10, player.State.CurrentShieldPoints[face]);
		Assert.Equal(0, player.State.ShieldDynamoGrantedPerFace);
	}

	[Fact]
	public void StateClone_PreservesShieldDynamoFields()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.ShieldDynamo, state.Id));
		state.ShieldDynamoGrantedPerFace = 15;

		var clone = state.Clone();

		Assert.True(BattleStatuses.HasShieldDynamo(clone));
		Assert.Equal(15, clone.ShieldDynamoGrantedPerFace);
	}

	private static (Unit Player, BattleOrchestrator Battle) PlayerWithShieldDynamoBattle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.ShieldDynamo]),
		};
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			pilotsByActorId: pilots);
		return (player, battle);
	}
}
