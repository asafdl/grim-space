using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.PilotCapabilities;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.Tests.World.StarSystem.PilotSkills;

public sealed class OptimalityContractCreditBonusTests
{
	private static readonly IReadOnlyList<PilotCapabilityGrant> Grants =
	[
		new(
			EPilotCapabilityId.OptimalityContractCreditBonus,
			"pilot-a",
			new PilotCapabilitySource(
				EPilotCapabilitySourceKind.Background,
				EPilotBackgroundId.OptimalityEnforcer.ToString())),
	];

	[Fact]
	public void Pipeline_IncreasesOptimalityContractCreditsOnly()
	{
		var pipeline = StarMapPilotCapabilityPipelineFactory.Build(Grants);
		IAction action = new CompleteContractAction(
			"player",
			"contract-a",
			EFaction.TheOptimality,
			ResourceBundle.Empty);
		var effect = new ChangeResourceEffect(
			TransactionSource.ContractPayment,
			ResourceBundle.Create(
				(ResourceId.Credits, 100),
				(ResourceId.IndustrialCore, 1)));

		var modified = Assert.IsType<ChangeResourceEffect>(
			Assert.Single(pipeline.Apply(
				action,
				StarMap.Create(1),
				new ActorRuntime(),
				[effect])));

		Assert.True(modified.Change.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(105, credits);
		Assert.True(modified.Change.TryGet(ResourceId.IndustrialCore, out var cores));
		Assert.Equal(1, cores);
	}

	[Fact]
	public void Pipeline_DoesNotModifyOtherFactionContract()
	{
		var pipeline = StarMapPilotCapabilityPipelineFactory.Build(Grants);
		IAction action = new CompleteContractAction(
			"player",
			"contract-a",
			EFaction.Pirates,
			ResourceBundle.Empty);
		var effect = new ChangeResourceEffect(
			TransactionSource.ContractPayment,
			ResourceBundle.Of(ResourceId.Credits, 100));

		var result = Assert.Single(pipeline.Apply(
			action,
			StarMap.Create(1),
			new ActorRuntime(),
			[effect]));

		Assert.Same(effect, result);
	}
}
