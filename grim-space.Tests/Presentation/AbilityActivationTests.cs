using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

public sealed class AbilityActivationTests
{
	private const string ActorId = "player";

	[Theory]
	[InlineData(EType.Fighter)]
	[InlineData(EType.Carrier)]
	[InlineData(EType.Patrol)]
	[InlineData(EType.Torpedo)]
	public void EveryRegisteredAbilityResolvesActivation(EType type)
	{
		foreach (var spec in AbilityHudCatalog.ForUnit(type))
		{
			var activation = AbilityActivation.For(spec.Def);
			Assert.NotNull(activation);
		}
	}

	[Theory]
	[InlineData(typeof(RailgunDef))]
	[InlineData(typeof(SpawnPatrolDef))]
	[InlineData(typeof(DetonateDef))]
	public void ActorOnlyAbilitiesRequireNoSelection(Type defType)
	{
		var def = (IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>)defType
			.GetProperty("Instance")!
			.GetValue(null)!;
		var activation = AbilityActivation.For(def);

		Assert.True(activation.HasRequiredSelection(null));
	}

	[Fact]
	public void MountedAbilitiesRequireStagedOrientation()
	{
		var activation = AbilityActivation.For(FlakDef.Instance);

		Assert.False(activation.HasRequiredSelection(null));
		Assert.True(activation.HasRequiredSelection(ESpatialOrientation.Port));
	}

	[Theory]
	[InlineData(ESpatialOrientation.Retro)]
	[InlineData(ESpatialOrientation.Dorsal)]
	[InlineData(ESpatialOrientation.Ventral)]
	public void TorpedoSelectionAcceptsAnyOrientationShape(ESpatialOrientation mountedOn)
	{
		var activation = AbilityActivation.For(TorpedoDef.Instance);

		Assert.True(activation.HasRequiredSelection(mountedOn));
	}

	[Fact]
	public void MountedConfirmationBuildsCorrectOrientation()
	{
		var action = Assert.IsType<FlakAction>(
			AbilityActivation.For(FlakDef.Instance)
				.Build(ActorId, ESpatialOrientation.Starboard));

		Assert.Equal(ActorId, action.ActorId);
		Assert.Equal(ESpatialOrientation.Starboard, action.MountedOn);
	}

	[Fact]
	public void ActorOnlyConfirmationBuildsCorrectAction()
	{
		var action = Assert.IsType<RailgunAction>(
			AbilityActivation.For(RailgunDef.Instance).Build(ActorId, null));

		Assert.Equal(ActorId, action.ActorId);
	}

	[Fact]
	public void InstructionHiddenWhenNotVisible()
	{
		var activation = AbilityActivation.For(RailgunDef.Instance);
		var instruction = activation.ResolveInstruction(
			visible: false,
			stagedMountedOn: null,
			capabilityIsLegal: true,
			confirmationError: null);

		Assert.False(instruction.Visible);
	}

	[Fact]
	public void MountedWaitingInstructionUsesSelectCopy()
	{
		var activation = AbilityActivation.For(FlakDef.Instance);
		var instruction = activation.ResolveInstruction(
			visible: true,
			stagedMountedOn: null,
			capabilityIsLegal: true,
			confirmationError: null);

		Assert.True(instruction.Visible);
		Assert.False(instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.SelectFiringDirection, instruction.Label);
	}

	[Fact]
	public void ReadyInstructionUsesConfirmCopy()
	{
		var activation = AbilityActivation.For(RailgunDef.Instance);
		var instruction = activation.ResolveInstruction(
			visible: true,
			stagedMountedOn: null,
			capabilityIsLegal: true,
			confirmationError: null);

		Assert.True(instruction.Visible);
		Assert.True(instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.ConfirmAction, instruction.Label);
	}
}
