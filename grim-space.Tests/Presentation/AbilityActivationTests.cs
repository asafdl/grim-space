using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

public sealed class AbilityActivationTests
{
	[Theory]
	[InlineData(EType.Fighter)]
	[InlineData(EType.Carrier)]
	[InlineData(EType.Patrol)]
	[InlineData(EType.Torpedo)]
	public void EveryRegisteredAbilityResolvesActivation(EType type)
	{
		foreach (var spec in AbilityHudCatalog.ForUnit(type))
			Assert.NotNull(spec.Targeting);
	}

	[Fact]
	public void ResolveChoicesFiltersCapabilitiesAndPlacesFlakMounts()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var port = new FlakAction(actor.Id, ESpatialOrientation.Port);
		var starboard = new FlakAction(actor.Id, ESpatialOrientation.Starboard);
		IAction[] capabilities = [port, new RailgunAction(actor.Id), starboard];

		var spec = Spec(EPlayerMode.Flak);
		var choices = AbilityActivation.ResolveChoices(spec, actor, capabilities);

		var frame = BodyFrame.From(actor);
		Assert.Collection(
			choices,
			choice =>
			{
				Assert.Same(port, choice.Action);
				Assert.Equal(actor.Position + frame.Step(ESpatialOrientation.Port), choice.Position);
				Assert.Equal(ESpatialOrientation.Port, choice.MountedOn);
				Assert.Same(spec.Targeting, choice.Targeting);
			},
			choice =>
			{
				Assert.Same(starboard, choice.Action);
				Assert.Equal(actor.Position + frame.Step(ESpatialOrientation.Starboard), choice.Position);
				Assert.Equal(ESpatialOrientation.Starboard, choice.MountedOn);
				Assert.Same(spec.Targeting, choice.Targeting);
			});
	}

	[Fact]
	public void ResolveChoicesUsesTorpedoLaunchPose()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var action = new TorpedoAction(actor.Id, ESpatialOrientation.Dorsal, "torpedo");

		var spec = Spec(EPlayerMode.Torpedo);
		var choice = Assert.Single(
			AbilityActivation.ResolveChoices(spec, actor, [action]));
		var pose = TorpedoMount.LaunchPose(actor, ESpatialOrientation.Dorsal);

		Assert.Same(action, choice.Action);
		Assert.Equal(pose.Position, choice.Position);
		Assert.Equal(pose.Fore, choice.Fore);
		Assert.Equal(pose.Dorsal, choice.Dorsal);
		Assert.Same(spec.Targeting, choice.Targeting);
	}

	[Fact]
	public void ResolveChoicesPlacesActorOnlyAbilitySources()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var railgun = new RailgunAction(actor.Id);
		var patrol = new SpawnPatrolAction(actor.Id, "patrol");
		var detonate = new DetonateAction(actor.Id);
		var patrolPose = PatrolBayMount.LaunchPose(actor);

		var railgunSpec = Spec(EPlayerMode.Railgun);
		var patrolSpec = Spec(EPlayerMode.SpawnPatrol);
		var detonateSpec = AbilityHudCatalog.ForUnit(EType.Torpedo).Single();
		var railgunChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				railgunSpec,
				actor,
				[railgun, patrol, detonate]));
		var patrolChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				patrolSpec,
				actor,
				[railgun, patrol, detonate]));
		var detonateChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				detonateSpec,
				actor,
				[railgun, patrol, detonate]));

		Assert.Equal(actor.Position + actor.Fore, railgunChoice.Position);
		Assert.Same(railgunSpec.Targeting, railgunChoice.Targeting);
		Assert.Equal(patrolPose.Position, patrolChoice.Position);
		Assert.Same(patrolSpec.Targeting, patrolChoice.Targeting);
		Assert.Equal(actor.Position, detonateChoice.Position);
		Assert.Same(detonateSpec.Targeting, detonateChoice.Targeting);
	}

	[Fact]
	public void ExecutionRebindsSpawnActionsWithFreshIds()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var previewTorpedo = new TorpedoAction(
			actor.Id,
			ESpatialOrientation.Retro,
			"__preview_torpedo__");
		var previewPatrol = new SpawnPatrolAction(actor.Id, "__preview_patrol__");
		var torpedoChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				Spec(EPlayerMode.Torpedo),
				actor,
				[previewTorpedo]));
		var patrolChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				Spec(EPlayerMode.SpawnPatrol),
				actor,
				[previewPatrol]));

		var torpedo = Assert.IsType<TorpedoAction>(
			AbilityActivation.CreateExecutionAction(torpedoChoice));
		var patrol = Assert.IsType<SpawnPatrolAction>(
			AbilityActivation.CreateExecutionAction(patrolChoice));

		Assert.NotEqual(previewTorpedo.SpawnedUnitId, torpedo.SpawnedUnitId);
		Assert.NotEqual(previewPatrol.SpawnedUnitId, patrol.SpawnedUnitId);
		Assert.Equal(previewTorpedo.MountedOn, torpedo.MountedOn);
	}

	private static AbilityHudCatalog.Spec Spec(EPlayerMode mode) =>
		AbilityHudCatalog.ForUnit(EType.Fighter)
			.Concat(AbilityHudCatalog.ForUnit(EType.Carrier))
			.First(spec => spec.Mode == mode);
}
