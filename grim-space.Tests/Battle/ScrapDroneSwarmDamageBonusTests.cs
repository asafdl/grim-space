using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class ScrapDroneSwarmDamageBonusTests
{
	[Fact]
	public void Pipeline_AddsOneDamageToTaggedScrapDroneAttack()
	{
		var pipeline = BattlePilotCapabilityPipelineFactory.Build(
			new HashSet<EPilotCapabilityId>
			{
				EPilotCapabilityId.ScrapDroneSwarmDamageBonus,
			});
		IAction action = new ScrapDroneSwarmAction("pilot-ship", ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(1, 1, 0) },
			(3 * 100),
			new Coord(0, 0, 0));

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(pipeline.Apply(action, null!, ActorRuntime.From(null), [effect])));

		Assert.Equal((4 * 100), modified.Damage);
		Assert.Contains(BattleActionTags.Attack, action.Tags);
		Assert.Contains(BattleActionTags.ScrapDrone, action.Tags);
	}
}
