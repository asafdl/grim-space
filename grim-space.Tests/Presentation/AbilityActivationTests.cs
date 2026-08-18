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
	public void ActorOnlyAbilitiesConfirmImmediately(Type defType)
	{
		var def = (IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>)defType
			.GetProperty("Instance")!
			.GetValue(null)!;
		var activation = AbilityActivation.For(def);

		Assert.True(activation.CanConfirm(null));
		Assert.NotNull(activation.Build(ActorId, null));
	}

	[Theory]
	[InlineData(typeof(FlakDef))]
	[InlineData(typeof(TorpedoDef))]
	public void MountedAbilitiesRequireStagedOrientation(Type defType)
	{
		var def = (IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>)defType
			.GetProperty("Instance")!
			.GetValue(null)!;
		var activation = AbilityActivation.For(def);

		Assert.False(activation.CanConfirm(null));
		Assert.Null(activation.Build(ActorId, null));

		Assert.True(activation.CanConfirm(ESpatialOrientation.Port));
		Assert.IsAssignableFrom<IAction>(activation.Build(ActorId, ESpatialOrientation.Port));
	}

	[Fact]
	public void MountedConfirmationBuildsCorrectOrientation()
	{
		var activation = AbilityActivation.For(FlakDef.Instance);
		var action = Assert.IsType<FlakAction>(activation.Build(ActorId, ESpatialOrientation.Starboard));

		Assert.Equal(ActorId, action.ActorId);
		Assert.Equal(ESpatialOrientation.Starboard, action.MountedOn);
	}

	[Fact]
	public void ActorOnlyConfirmationBuildsCorrectAction()
	{
		var activation = AbilityActivation.For(RailgunDef.Instance);
		var action = Assert.IsType<RailgunAction>(activation.Build(ActorId, null));

		Assert.Equal(ActorId, action.ActorId);
	}

	[Fact]
	public void InstructionHiddenWhenNotVisible()
	{
		var activation = AbilityActivation.For(RailgunDef.Instance);
		var instruction = activation.ResolveInstruction(visible: false, stagedMountedOn: null);

		Assert.False(instruction.Visible);
	}

	[Fact]
	public void MountedWaitingInstructionUsesSelectCopy()
	{
		var activation = AbilityActivation.For(FlakDef.Instance);
		var instruction = activation.ResolveInstruction(visible: true, stagedMountedOn: null);

		Assert.True(instruction.Visible);
		Assert.False(instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.SelectFiringDirection, instruction.Label);
	}

	[Fact]
	public void ReadyInstructionUsesConfirmCopy()
	{
		var activation = AbilityActivation.For(RailgunDef.Instance);
		var instruction = activation.ResolveInstruction(visible: true, stagedMountedOn: null);

		Assert.True(instruction.Visible);
		Assert.True(instruction.CanConfirm);
		Assert.Equal(BattleHudCopy.ConfirmAction, instruction.Label);
	}
}
