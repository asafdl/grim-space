using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Tests.Movement;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ShieldsUpActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ShieldsUp_IsLegalWhenEquippedWithApAndNotUsedThisTurn()
	{
		var player = PlayerWithShieldsUp();
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);

		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId);

		Assert.Contains(legal, action => action is ShieldsUpAction);
	}

	[Fact]
	public void ShieldsUp_SchedulesRestoreAndApPenaltyThenAppliesOnUpkeep()
	{
		var player = PlayerWithShieldsUp();
		player.State.ShieldPoints[ESpatialOrientation.Forward] = 0;
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);

		BattleTestApply.TryApplyOne(
			ShieldsUpDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(0, player.State.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(1, player.State.PendingShieldRestore[ESpatialOrientation.Forward]);
		Assert.Equal(ShieldsUpDef.NextUpkeepApPenalty, player.State.PendingApPenalty);
		Assert.True(player.State.ShieldsUpUsedThisTurn);

		new RoundUpkeepEffect().Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(1, player.State.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(0, player.State.PendingShieldRestore[ESpatialOrientation.Forward]);
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - ShieldsUpDef.NextUpkeepApPenalty,
			player.State.ActionPoints);
		Assert.False(player.State.ShieldsUpUsedThisTurn);
	}

	[Fact]
	public void ShieldsUp_RestoreIsCappedAtLoadoutMax()
	{
		var player = PlayerWithShieldsUp();
		player.State.ShieldPoints[ESpatialOrientation.Forward] = 1;
		player.State.PendingShieldRestore[ESpatialOrientation.Forward] = 3;
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);

		new RoundUpkeepEffect().Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(1, player.State.ShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void ShieldsUpActivateEffect_UndoRestoresPendingState()
	{
		var player = PlayerWithShieldsUp();
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var effect = new ShieldsUpActivateEffect();

		effect.Apply(battle.Engine.World, runtime, PlayerId);
		effect.Undo(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(0, player.State.PendingApPenalty);
		Assert.False(player.State.ShieldsUpUsedThisTurn);
	}

	[Fact]
	public void AbilityDefsFor_IncludesShieldsUpWhenEquipped()
	{
		var player = PlayerWithShieldsUp();

		var defs = Capabilities.AbilityDefsFor(player.State);

		Assert.Contains(ShieldsUpDef.Instance, defs);
	}

	[Fact]
	public void BattleUnitStateDto_RoundTripsPilotSkillRuntime()
	{
		var player = PlayerWithShieldsUp();
		player.State.PendingApPenalty = 2;
		player.State.PendingShieldRestore[ESpatialOrientation.Port] = 1;
		player.State.ShieldsUpUsedThisTurn = true;

		var dto = GrimSpace.Run.Persistence.SaveDtoMapper.CaptureBattleUnitState(player.State);
		var restored = GrimSpace.Run.Persistence.SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.Equal(player.State.PendingApPenalty, restored.PendingApPenalty);
		Assert.Equal(1, restored.PendingShieldRestore[ESpatialOrientation.Port]);
		Assert.True(restored.ShieldsUpUsedThisTurn);
		Assert.Equal(player.State.PilotSkills, restored.PilotSkills);
	}

	private static Unit PlayerWithShieldsUp() =>
		Factory.Create(
			ShipInstance.FromCatalog(PlayerId, EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Player),
			Coord.Forward,
			Coord.Up,
			pilotSkills: [EPilotSkillId.ShieldsUp]);
}
