using GrimSpace.Battle.Actions.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Actions;

public sealed class MissileAction(Coord center, EMissileMount mount) : IAction
{
	public Coord Center { get; } = center;
	public EMissileMount Mount { get; } = mount;

	public int GetApCost(State player) => 0;

	public IReadOnlyList<IStateEffect> Resolve(ActionBoard board) =>
		[new SpawnHazardEffect(Center)];
}
