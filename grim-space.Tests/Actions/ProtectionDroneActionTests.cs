using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ProtectionDroneActionTests
{
	private const string PlayerId = "player";
	private const string AllyId = "ally";

	[Fact]
	public void Discover_YieldsAllyTargetsOnly()
	{
		var (player, ally, battle) = BattleWithAlly();
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var discovered = ProtectionDroneDef.Instance.Discover(battle.Engine.World, runtime, PlayerId).ToArray();

		Assert.Equal([AllyId], discovered.OfType<ProtectionDroneAction>().Select(action => action.TargetId));
		Assert.False(ProtectionDroneDef.Instance.IsLegal(
			ProtectionDroneDef.Instance.Bind(PlayerId, PlayerId),
			battle.Engine.World,
			runtime));
		Assert.False(ProtectionDroneDef.Instance.IsLegal(
			ProtectionDroneDef.Instance.Bind(PlayerId, "enemy"),
			battle.Engine.World,
			runtime));
		_ = player;
		_ = ally;
	}

	[Fact]
	public void Apply_GrantsBuffOnAllySchedulesExpiryAndSetsCooldown()
	{
		var (player, ally, battle) = BattleWithAlly();

		BattleTestApply.TryApplyOne(
			ProtectionDroneDef.Instance.Bind(PlayerId, AllyId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.True(ally.State.ProtectionDroneBuffActive);
		Assert.False(player.State.ProtectionDroneBuffActive);
		Assert.Equal(
			PilotSkillPercents.ProtectionDrone.CooldownTurns,
			player.State.ProtectionDroneCooldownRemaining);
		Assert.Contains(
			battle.Engine.World.ActivationRoundLists.EntriesFor(AllyId),
			entry => entry.Action is ClearProtectionDroneBuffAction clear
				&& clear.ActorId == AllyId
				&& entry.RoundsRemaining == PilotSkillPercents.ProtectionDrone.BuffDurationEndPasses);

		BattleTestApply.CommitActivationRoundEndPasses(
			battle,
			AllyId,
			PilotSkillPercents.ProtectionDrone.BuffDurationEndPasses);

		Assert.False(ally.State.ProtectionDroneBuffActive);
	}

	[Fact]
	public void Apply_IllegalWhileOnCooldownOrTargetAlreadyBuffed()
	{
		var (_, ally, battle) = BattleWithAlly();
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var world = battle.Engine.World;
		var action = ProtectionDroneDef.Instance.Bind(PlayerId, AllyId);

		BattleTestApply.TryApplyOne(action, world, runtime, PlayerId);

		Assert.False(ProtectionDroneDef.Instance.IsLegal(action, world, runtime));

		world.StateOf(PlayerId).ProtectionDroneCooldownRemaining = 0;
		Assert.False(ProtectionDroneDef.Instance.IsLegal(action, world, runtime));

		ally.State.ProtectionDroneBuffActive = false;
		Assert.True(ProtectionDroneDef.Instance.IsLegal(action, world, runtime));
	}

	[Fact]
	public void Cooldown_TicksDownOnTurnUpkeep()
	{
		var (player, _, battle) = BattleWithAlly();
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		BattleTestApply.TryApplyOne(
			ProtectionDroneDef.Instance.Bind(PlayerId, AllyId),
			battle.Engine.World,
			runtime,
			PlayerId);

		new TurnUpkeepEffect().Apply(battle.Engine.World, runtime, PlayerId);
		Assert.Equal(
			PilotSkillPercents.ProtectionDrone.CooldownTurns - 1,
			player.State.ProtectionDroneCooldownRemaining);
	}

	[Fact]
	public void HudCatalog_ExposesTargetPickForProtectionDrone()
	{
		var (player, _, battle) = BattleWithAlly();
		var specs = AbilityHudCatalog.ForActor(
			player.State,
			battle.Engine.ActorRuntimes.For(PlayerId));

		var spec = Assert.Single(specs, row => row.Mode == EPlayerMode.ProtectionDrone);
		Assert.NotNull(spec.TargetPick);
		Assert.Same(ProtectionDroneDef.Instance, spec.Def);
		Assert.Equal(1, spec.TargetPick!.MinPicks);
		Assert.Equal(1, spec.TargetPick.MaxPicks);
	}

	[Fact]
	public void LegalCapabilities_IncludesProtectionDroneWhenAllyExists()
	{
		var (_, _, battle) = BattleWithAlly();

		Assert.Contains(
			Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId),
			action => action is ProtectionDroneAction);
	}

	private static (Unit Player, Unit Ally, BattleOrchestrator Battle) BattleWithAlly()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog(PlayerId, EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Player));
		var ally = BattleTestFixture.Ally(new Coord(6, 5, 5), AllyId);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.ProtectionDrone]),
		};
		var battle = BattleTestFixture.BeginSimulation(
			player,
			[ally, enemy],
			pilotsByActorId: pilots);
		return (player, ally, battle);
	}
}
