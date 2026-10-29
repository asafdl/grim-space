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
public sealed class BraceActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Brace_SchedulesReleaseOnActivationRoundEndPasses()
	{
		var (player, battle) = PlayerWithBraceBattle();
		SetSymmetricShields(player.State, 100);
		player.State.ShieldPoints[ESpatialOrientation.Port] = 50;

		BattleTestApply.TryApplyOne(
			BraceDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.True(player.State.ShieldBraceActive);
		Assert.True(player.State.ShieldBraceUsedThisTurn);
		Assert.Contains(
			battle.Engine.World.ActivationRoundLists.EntriesFor(PlayerId),
			entry => entry.Action is ReleaseShieldBraceAction);

		BattleTestApply.CommitActivationRoundEndPasses(battle, PlayerId, BraceDef.ExpiryEndPasses);

		Assert.False(player.State.ShieldBraceActive);
		Assert.True(player.State.ShieldBraceUsedThisTurn);

		battle.Engine.AdvanceTick();

		Assert.False(player.State.ShieldBraceUsedThisTurn);
	}

	[Fact]
	public void Brace_ConsolidatesSixtySixPercentAndAllowsOverflow()
	{
		var (player, battle) = PlayerWithBraceBattle();
		SetSymmetricShields(player.State, 100);
		var max = player.State.Loadout.MaxShieldPoints[ESpatialOrientation.Forward];

		BattleTestApply.TryApplyOne(
			BraceDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(34, player.State.ShieldPoints[ESpatialOrientation.Port]);
		Assert.True(player.State.ShieldPoints[ESpatialOrientation.Forward] > max);
	}

	[Fact]
	public void Brace_RedistributesShieldDamageToOtherFacesOnRelease()
	{
		var (player, battle) = PlayerWithBraceBattle(EType.Fighter);
		player.State.ShieldPoints[ESpatialOrientation.Forward] = 100;
		player.State.ShieldPoints[ESpatialOrientation.Port] = 100;

		BattleTestApply.TryApplyOne(
			BraceDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(34, player.State.ShieldPoints[ESpatialOrientation.Port]);
		Defense.AbsorbWithShields(
			player.State,
			ESpatialOrientation.Forward,
			40);

		Assert.Equal(40, player.State.ShieldBraceDamagePending);

		BattleTestApply.CommitActivationRoundEndPasses(battle, PlayerId, BraceDef.ExpiryEndPasses);

		Assert.False(player.State.ShieldBraceActive);
		Assert.Equal(0, player.State.ShieldBraceDamagePending);
		Assert.True(player.State.ShieldPoints[ESpatialOrientation.Port] < 100);
	}

	private static void SetSymmetricShields(State state, int amount)
	{
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			state.ShieldPoints[face] = amount;
	}

	private static (Unit Player, BattleOrchestrator Battle) PlayerWithBraceBattle(
		EType chassis = EType.RepurposedMiner)
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog(PlayerId, chassis),
			ETeam.Player,
			new Coord(5, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Player));
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.Brace]),
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
		return (player, battle);
	}
}
