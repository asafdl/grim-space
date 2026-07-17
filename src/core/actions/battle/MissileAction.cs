using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Core.Actions.Battle.Rules;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MissileAction(string ownerId, Coord center, EMissileMount mount, int range) : IAction, IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public Coord Center { get; } = center;
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) =>
		BattleRuleEnforcer.AllSatisfied(Rules, board, context);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context) =>
	[
		new SpawnHazardEffect(Center),
		new MissileChangeEffect(-1),
	];

	private IEnumerable<IBattleRule> Rules =>
	[
		new HasMissileAmmoRule(OwnerId),
		new MissileTargetingRule(OwnerId, Center, Mount, Range),
	];
}
