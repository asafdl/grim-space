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

[BattleTestSuite]
public sealed class AbilityActivationTests
{
	[Theory]
	[InlineData(EType.Fighter)]
	[InlineData(EType.Carrier)]
	[InlineData(EType.RepurposedMiner)]
	[InlineData(EType.VoidBomb)]
	public void IconTintIsOpaqueAndMatchesTargetingTint(EType type)
	{
		foreach (var spec in AbilityHudCatalog.ForUnit(type))
		{
			Assert.Equal(1f, spec.IconTint.A);
			Assert.Equal(spec.Targeting.Tint.R, spec.IconTint.R);
			Assert.Equal(spec.Targeting.Tint.G, spec.IconTint.G);
			Assert.Equal(spec.Targeting.Tint.B, spec.IconTint.B);
		}
	}

	[Theory]
	[InlineData(EType.Fighter)]
	[InlineData(EType.Carrier)]
	[InlineData(EType.RepurposedMiner)]
	[InlineData(EType.VoidBomb)]
	public void EveryRegisteredAbilityResolvesActivation(EType type)
	{
		foreach (var spec in AbilityHudCatalog.ForUnit(type))
			Assert.NotNull(spec.Targeting);
	}

	[Fact]
	public void ResolveChoicesFiltersCapabilitiesAndPlacesScrapDroneSwarmMounts()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var port = new ScrapDroneSwarmAction(actor.Id, ESpatialOrientation.Port);
		var starboard = new ScrapDroneSwarmAction(actor.Id, ESpatialOrientation.Starboard);
		IAction[] capabilities = [port, new LightningCannonAction(actor.Id), starboard];

		var spec = Spec(EPlayerMode.ScrapDroneSwarm);
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
	public void ResolveChoicesPlacesGunshipLightningCannonMounts()
	{
		var actor = State.FromShipInstance(
			GrimSpace.Units.ShipInstance.FromCatalog("gunship", EType.Gunship),
			new Coord(5, 5, 5));
		var port = new LightningCannonAction(actor.Id, ESpatialOrientation.Port);
		var starboard = new LightningCannonAction(actor.Id, ESpatialOrientation.Starboard);
		var spec = AbilityHudCatalog.ForUnit(EType.Gunship)
			.Single(entry => entry.Mode == EPlayerMode.LightningCannon);

		var choices = AbilityActivation.ResolveChoices(
			spec,
			actor,
			[port, starboard]);

		var frame = BodyFrame.From(actor);
		Assert.Collection(
			choices,
			choice =>
			{
				Assert.Same(port, choice.Action);
				Assert.Equal(actor.Position + frame.Step(ESpatialOrientation.Port), choice.Position);
				Assert.Equal(ESpatialOrientation.Port, choice.MountedOn);
			},
			choice =>
			{
				Assert.Same(starboard, choice.Action);
				Assert.Equal(actor.Position + frame.Step(ESpatialOrientation.Starboard), choice.Position);
				Assert.Equal(ESpatialOrientation.Starboard, choice.MountedOn);
			});
	}

	[Fact]
	public void ResolveChoicesUsesTorpedoLaunchPose()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var action = new VoidBombAction(actor.Id, ESpatialOrientation.Dorsal, "void_bomb");

		var spec = Spec(EPlayerMode.VoidBomb);
		var choice = Assert.Single(
			AbilityActivation.ResolveChoices(spec, actor, [action]));
		var pose = VoidBombMount.LaunchPose(actor, ESpatialOrientation.Dorsal);

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
		var lightningCannon = new LightningCannonAction(actor.Id);
		var repurposedMiner = new SpawnRepurposedMinerAction(actor.Id, ESpatialOrientation.Ventral, "repurposedMiner");
		var detonate = new DetonateAction(actor.Id);
		var repurposedMinerPose = MinerBayMount.LaunchPose(actor, ESpatialOrientation.Ventral);

		var lightningCannonSpec = Spec(EPlayerMode.LightningCannon);
		var repurposedMinerSpec = Spec(EPlayerMode.SpawnRepurposedMiner);
		var detonateSpec = AbilityHudCatalog.ForUnit(EType.VoidBomb).Single();
		var lightningCannonChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				lightningCannonSpec,
				actor,
				[lightningCannon, repurposedMiner, detonate]));
		var repurposedMinerChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				repurposedMinerSpec,
				actor,
				[lightningCannon, repurposedMiner, detonate]));
		var detonateChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				detonateSpec,
				actor,
				[lightningCannon, repurposedMiner, detonate]));

		Assert.Equal(
			actor.Position + BodyFrame.From(actor).Step(lightningCannon.MountedOn),
			lightningCannonChoice.Position);
		Assert.Equal(lightningCannon.MountedOn, lightningCannonChoice.MountedOn);
		Assert.Same(lightningCannonSpec.Targeting, lightningCannonChoice.Targeting);
		Assert.Equal(repurposedMinerPose.Position, repurposedMinerChoice.Position);
		Assert.Same(repurposedMinerSpec.Targeting, repurposedMinerChoice.Targeting);
		Assert.Equal(actor.Position, detonateChoice.Position);
		Assert.Same(detonateSpec.Targeting, detonateChoice.Targeting);
	}

	[Fact]
	public void ExecutionRebindsSpawnActionsWithFreshIds()
	{
		var actor = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		var previewTorpedo = new VoidBombAction(
			actor.Id,
			ESpatialOrientation.Retro,
			"__preview_void_bomb__");
		var previewRepurposedMiner = new SpawnRepurposedMinerAction(
			actor.Id,
			ESpatialOrientation.Ventral,
			"__preview_repurposed_miner__");
		var torpedoChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				Spec(EPlayerMode.VoidBomb),
				actor,
				[previewTorpedo]));
		var repurposedMinerChoice = Assert.Single(
			AbilityActivation.ResolveChoices(
				Spec(EPlayerMode.SpawnRepurposedMiner),
				actor,
				[previewRepurposedMiner]));

		var torpedo = Assert.IsType<VoidBombAction>(
			AbilityActivation.CreateExecutionAction(torpedoChoice));
		var repurposedMiner = Assert.IsType<SpawnRepurposedMinerAction>(
			AbilityActivation.CreateExecutionAction(repurposedMinerChoice));

		Assert.NotEqual(previewTorpedo.SpawnedUnitId, torpedo.SpawnedUnitId);
		Assert.NotEqual(previewRepurposedMiner.SpawnedUnitId, repurposedMiner.SpawnedUnitId);
		Assert.Equal(previewTorpedo.MountedOn, torpedo.MountedOn);
		Assert.Equal(previewRepurposedMiner.MountedOn, repurposedMiner.MountedOn);
	}

	private static AbilityHudCatalog.Spec Spec(EPlayerMode mode) =>
		AbilityHudCatalog.ForUnit(EType.Fighter)
			.Concat(AbilityHudCatalog.ForUnit(EType.Carrier))
			.First(spec => spec.Mode == mode);
}
