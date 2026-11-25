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
	[Fact]
	public void IncreasesOptimalityContractCreditsOnly()
	{
		var effect = new ChangeResourceEffect(
			TransactionSource.ContractPayment,
			ResourceBundle.Create(
				(ResourceId.Credits, 100),
				(ResourceId.IndustrialCore, 1)));

		var modified = Assert.IsType<ChangeResourceEffect>(Prepare(EFaction.TheOptimality, effect));

		Assert.True(modified.Change.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(105, credits);
		Assert.True(modified.Change.TryGet(ResourceId.IndustrialCore, out var cores));
		Assert.Equal(1, cores);
	}

	[Fact]
	public void DoesNotModifyOtherFactionContract()
	{
		var effect = new ChangeResourceEffect(
			TransactionSource.ContractPayment,
			ResourceBundle.Of(ResourceId.Credits, 100));

		Assert.Same(effect, Prepare(EFaction.Pirates, effect));
	}

	private static IEffect<StarMap, ActorRuntime> Prepare(EFaction issuer, IEffect<StarMap, ActorRuntime> effect)
	{
		var runtime = new ActorRuntime();
		runtime.ReplaceStrategicCapabilities(
		[
			new PilotCapabilityGrant(
				EPilotCapabilityId.OptimalityContractCreditBonus,
				"pilot-a",
				new PilotCapabilitySource(
					EPilotCapabilitySourceKind.Background,
					EPilotBackgroundId.OptimalityEnforcer.ToString())),
		]);
		return StarMapPilotCapabilities.Rules.Prepare(
			new CompleteContractAction("player", "contract-a", issuer, ResourceBundle.Empty),
			StarMap.Create(1),
			runtime,
			effect);
	}
}
