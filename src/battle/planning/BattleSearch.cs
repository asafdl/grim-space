using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Planning;

public static class BattleSearch
{
	public static SearchVisitState MoveVisit(
		BattleBoard world,
		ActorRuntimes<ActorSession> runtimes,
		string actorId) =>
		BattleSearchVisit.ForMove(world, runtimes.For(actorId), actorId);

	public static SearchVisitState TurnVisit(
		BattleBoard world,
		ActorRuntimes<ActorSession> runtimes,
		string actorId) =>
		BattleSearchVisit.ForTurn(world, runtimes.For(actorId), actorId);
}
