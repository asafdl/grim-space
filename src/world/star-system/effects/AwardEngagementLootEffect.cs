using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class AwardEngagementLootEffect(
	IReadOnlyList<LootRoll> rolls,
	ResourceBundle total) : IEffect<StarMap, ActorRuntime>
{
	private readonly ChangeResourceEffect _resources =
		new(TransactionSource.BattleLoot, total);

	public IReadOnlyList<LootRoll> Rolls { get; } = rolls;

	public ResourceBundle Total { get; } = total;

	public AwardEngagementLootEffect WithLoot(
		IReadOnlyList<LootRoll> replacementRolls,
		ResourceBundle replacementTotal)
	{
		ArgumentNullException.ThrowIfNull(replacementRolls);
		ArgumentNullException.ThrowIfNull(replacementTotal);
		return new AwardEngagementLootEffect(replacementRolls, replacementTotal);
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		_resources.Apply(world, runtime, actorId);

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		_resources.Undo(world, runtime, actorId);
}
