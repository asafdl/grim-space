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
	public void ShieldsUp_IsLegalWhenEquippedAndNotUsedThisTurn()
	{
		var player = PlayerWithShieldsUp();
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);

		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId);

		Assert.Contains(legal, action => action is ShieldsUpAction);
	}

	[Fact]
	public void ShieldsUp_SchedulesFollowUpsThenAppliesPenaltyOnNextTickAfterUpkeepRefill()
	{
		var player = PlayerWithShieldsUp();
		player.State.ShieldPoints[ESpatialOrientation.Forward] = 0;
		player.State.ActionPoints = 3;
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);

		BattleTestApply.TryApplyOne(
			ShieldsUpDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(3, player.State.ActionPoints);
		Assert.Equal(1, TemporaryShields.TotalOnFace(player.State, ESpatialOrientation.Forward));
		Assert.True(player.State.ShieldsUpUsedThisTurn);
		Assert.True(battle.Engine.World.Timeline.ContainsPending(
			action => action is ApDeltaAction ap && ap.ActorId == PlayerId && ap.Delta == -ShieldsUpDef.NextTickApPenalty));

		new RoundUpkeepEffect().Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
		Assert.True(player.State.ShieldsUpUsedThisTurn);

		battle.Engine.AdvanceTick();

		Assert.Equal(
			MovementExpectations.FighterApPerTurn - ShieldsUpDef.NextTickApPenalty,
			player.State.ActionPoints);
		Assert.False(player.State.ShieldsUpUsedThisTurn);
		Assert.Equal(0, TemporaryShields.TotalOnFace(player.State, ESpatialOrientation.Forward));
	}

	[Fact]
	public void ShieldsUp_TemporaryBonusCanExceedLoadoutMax()
	{
		var player = PlayerWithShieldsUp();
		var max = player.State.Loadout.MaxShieldPoints[ESpatialOrientation.Forward];
		player.State.ShieldPoints[ESpatialOrientation.Forward] = max;
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);

		BattleTestApply.TryApplyOne(
			ShieldsUpDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(max, player.State.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(1, TemporaryShields.TotalOnFace(player.State, ESpatialOrientation.Forward));
	}

	[Fact]
	public void ShieldsUpActivateEffect_UndoRestoresUsedFlag()
	{
		var player = PlayerWithShieldsUp();
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var effect = new ShieldsUpActivateEffect();

		effect.Apply(battle.Engine.World, runtime, PlayerId);
		effect.Undo(battle.Engine.World, runtime, PlayerId);

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
		TemporaryShields.GrantPerFace(player.State, "grant", 1);
		player.State.ShieldsUpUsedThisTurn = true;

		var dto = GrimSpace.Run.Persistence.SaveDtoMapper.CaptureBattleUnitState(player.State);
		var restored = GrimSpace.Run.Persistence.SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.Equal(1, TemporaryShields.TotalOnFace(restored, ESpatialOrientation.Port));
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
