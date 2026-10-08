using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class GoopGunActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ResolveSetsCooldownToUnavailableTurnsPlusOne()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			BattleSpawnTestKit.FighterWithGoopGun(PlayerId),
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));
		var spec = CatalogExpectations.DefaultGoopGunSpec();
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(
			spec.UnavailableTurns + 1,
			StateMountTestKit.CooldownRemaining(
				battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId),
				EAbilityKind.GoopGun,
				ESpatialOrientation.Forward));
	}

	[Fact]
	public void BindAllocatesTypedGoopHazardId()
	{
		var action = GoopGunDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);

		Assert.StartsWith($"{NonUnitTypeSlug.Goop}-", action.GoopHazardId);
	}

	[Fact]
	public void ExecutionRebindsPreviewGoopHazardId()
	{
		var preview = new GoopGunAction(
			PlayerId,
			ESpatialOrientation.Forward,
			Capabilities.PreviewGoopHazardId);
		var choice = new AbilityActivationChoice(
			preview,
			Coord.Zero,
			Coord.Forward,
			Coord.Up,
			AbilityHudCatalog.ForUnit(EType.Fighter)[0].Targeting);

		var execution = Assert.IsType<GoopGunAction>(AbilityActivation.CreateExecutionAction(choice));

		Assert.NotEqual(Capabilities.PreviewGoopHazardId, execution.GoopHazardId);
		Assert.StartsWith($"{NonUnitTypeSlug.Goop}-", execution.GoopHazardId);
	}

	[Fact]
	public void LegalCapabilitiesUsePreviewGoopHazardId()
	{
		var origin = new Coord(5, 5, 5);
		var player = Factory.Create(
			BattleSpawnTestKit.FighterWithGoopGun(PlayerId),
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));

		var goop = Assert.IsType<GoopGunAction>(
			Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId)
				.Single(action => action is GoopGunAction));

		Assert.Equal(Capabilities.PreviewGoopHazardId, goop.GoopHazardId);
	}
}
