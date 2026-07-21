using GrimSpace.Battle.Slices;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Actions;

internal static class BattleSliceFactory
{
	public static BattleSlices Create(
		BattleBoard world,
		TurnState state,
		Timeline timeline,
		string ownerId) =>
		SystemAction.Is(ownerId)
			? BattleSlices.ForSystem(world, timeline)
			: BattleSlices.For(world, ownerId, state, timeline);
}
