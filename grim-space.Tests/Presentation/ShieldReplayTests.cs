using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class ShieldReplayTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void TemporaryShieldGrantAndExpiryReplayTheResolvedFaceAmounts(bool percentOfMax)
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(player, BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		var state = player.State;
		state.ShieldPoints.Fill(0);
		var replay = ReplayFrom(state);
		var runtime = battle.Engine.ActorRuntimes.For(state.Id);
		var world = battle.Engine.World;
		var grant = percentOfMax
			? new GrantTemporaryShieldPercentOfFaceMaxEffect("first", 25).Apply(world, runtime, state.Id)
			: new GrantTemporaryShieldPerFaceEffect("first", 50).Apply(world, runtime, state.Id);
		var first = Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(grant)).Value;
		new GrantTemporaryShieldPerFaceEffect("second", 20).Apply(world, runtime, state.Id);
		var afterSecond = ShieldsChangedFacts.Capture(state);

		replay.ApplyShields(first);
		Assert.Single(TemporaryShields.ExtractGrants(replay.StateOf(state.Id)));
		Assert.Equal(0, replay.StateOf(state.Id).ShieldPoints.MaxOnAnyFace);
		replay.ApplyShields(afterSecond);
		AssertShieldsMatch(state, replay.StateOf(state.Id));

		var expiry = new RevokeTemporaryShieldGrantEffect("first").Apply(world, runtime, state.Id);
		replay.ApplyShields(Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(expiry)).Value);

		AssertShieldsMatch(state, replay.StateOf(state.Id));
		Assert.Equal(20, TemporaryShields.TotalOnFace(replay.StateOf(state.Id), ESpatialOrientation.Forward));
	}

	[Fact]
	public void BraceOverflowDamageAndReleaseReplayWithoutClampingOrRecalculatingTheAbility()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(player, BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		var state = player.State;
		state.ShieldPoints.Fill(100);
		var replay = ReplayFrom(state);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(state.Id);
		var records = new ActivateShieldBraceEffect(ESpatialOrientation.Forward).Apply(world, runtime, state.Id);

		replay.ApplyShields(Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(records)).Value);

		AssertShieldsMatch(state, replay.StateOf(state.Id));
		Assert.True(replay.StateOf(state.Id).ShieldPoints[ESpatialOrientation.Forward]
			> state.Loadout.MaxShieldPoints[ESpatialOrientation.Forward]);
		Assert.NotNull(ShieldBrace.TryPayload(replay.StateOf(state.Id)));
		var impact = new ImpactFacts("enemy", state.Id, EImpactCause.RamImpact, ESpatialOrientation.Forward, 40, 0);
		Defense.AbsorbWithShields(state, impact.Face, impact.ShieldDamage);
		replay.ApplyImpact(impact);
		Assert.Equal(40, ShieldBrace.TryPayload(replay.StateOf(state.Id))!.DamagePending);

		var release = new ReleaseShieldBraceEffect().Apply(world, runtime, state.Id);
		replay.ApplyShields(Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(release)).Value);

		AssertShieldsMatch(state, replay.StateOf(state.Id));
		Assert.Null(ShieldBrace.TryPayload(replay.StateOf(state.Id)));
	}

	[Fact]
	public void HealAfterTemporaryShieldDamageRestoresOnlyTheResolvedRegularFace()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var ally = BattleTestFixture.Ally(new Coord(5, 6, 5));
		var battle = BattleTestFixture.BeginSimulation(player, [ally, BattleTestFixture.Enemy(new Coord(0, 0, 0))]);
		var state = ally.State;
		state.ShieldPoints[ESpatialOrientation.Forward] = 0;
		var replay = ReplayFrom(state);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(state.Id);
		var grant = new GrantTemporaryShieldPerFaceEffect("shield", 20).Apply(world, runtime, state.Id);
		replay.ApplyShields(Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(grant)).Value);
		Defense.AbsorbWithShields(state, ESpatialOrientation.Forward, 10);
		replay.ApplyImpact(new ImpactFacts(
			"enemy", state.Id, EImpactCause.RamImpact, ESpatialOrientation.Forward, 10, 0));

		var records = new ApplyAreaAllyHealEffect(
			EImpactCause.ScrapDroneSwarmBurst, new HashSet<Coord> { state.Position }, 50)
			.Apply(world, runtime, player.State.Id);
		var heal = Assert.IsType<Record<HealFacts>>(Assert.Single(records)).Value;
		replay.ApplyHeal(heal);

		Assert.Equal(50, heal.ShieldRestored);
		Assert.Equal(ESpatialOrientation.Forward, heal.ShieldFace);
		AssertShieldsMatch(state, replay.StateOf(state.Id));
		Assert.Equal(10, TemporaryShields.TotalOnFace(replay.StateOf(state.Id), ESpatialOrientation.Forward));
	}

	[Fact]
	public void ExpiryOfMissingGrantAndReleaseWithoutBraceEmitNoShieldChange()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(player, BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		Assert.Empty(new RevokeTemporaryShieldGrantEffect("missing").Apply(battle.Engine.World, runtime, player.State.Id));
		Assert.Empty(new ReleaseShieldBraceEffect().Apply(battle.Engine.World, runtime, player.State.Id));
	}

	[Fact]
	public void ShieldFactsRemainIndependentOfEffectUndoAndReplayMutation()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(player, BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		var state = player.State;
		state.ShieldPoints.Fill(100);
		var replay = ReplayFrom(state);
		var runtime = battle.Engine.ActorRuntimes.For(state.Id);
		var world = battle.Engine.World;
		var grantEffect = new GrantTemporaryShieldPerFaceEffect("grant", 20);
		var grant = Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(grantEffect.Apply(world, runtime, state.Id))).Value;
		var braceEffect = new ActivateShieldBraceEffect(ESpatialOrientation.Forward);
		var brace = Assert.IsType<Record<ShieldsChangedFacts>>(Assert.Single(braceEffect.Apply(world, runtime, state.Id))).Value;
		braceEffect.Undo(world, runtime, state.Id);
		grantEffect.Undo(world, runtime, state.Id);

		Assert.Null(ShieldBrace.TryStatus(state));
		Assert.Empty(TemporaryShields.ExtractGrants(state));
		Assert.Equal(100, state.ShieldPoints[ESpatialOrientation.Forward]);
		replay.ApplyShields(grant);
		replay.ApplyShields(brace);
		var restored = replay.StateOf(state.Id);
		restored.ShieldPoints.Fill(0);
		TemporaryShields.AbsorbOnFace(restored, ESpatialOrientation.Port, 10);
		ShieldBrace.TryPayload(restored)!.DamagePending = 99;

		replay.ApplyShields(brace);

		Assert.Equal(brace.ShieldPoints[ESpatialOrientation.Forward], restored.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(20, TemporaryShields.TotalOnFace(restored, ESpatialOrientation.Port));
		Assert.Equal(0, ShieldBrace.TryPayload(restored)!.DamagePending);
	}

	private static ReplayState ReplayFrom(State state) =>
		new(new Dictionary<string, State> { [state.Id] = state });

	private static void AssertShieldsMatch(State expected, State actual)
	{
		Assert.True(expected.ShieldPoints.Matches(actual.ShieldPoints));
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			Assert.Equal(TemporaryShields.TotalOnFace(expected, face), TemporaryShields.TotalOnFace(actual, face));
	}
}
