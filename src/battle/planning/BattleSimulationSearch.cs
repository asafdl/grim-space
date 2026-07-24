using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Planning;

public static class BattleSimulationSearch
{
	public static IEnumerable<SearchFrame<BattleBoard, ActorSession>> SearchMoves(
		this Simulation<BattleBoard, ActorSession> session,
		string actorId) =>
		session.Search(actorId, [MoveDef.Instance], BattleSearch.MoveVisit);

	public static IEnumerable<SearchFrame<BattleBoard, ActorSession>> SearchTurn(
		this Simulation<BattleBoard, ActorSession> session,
		string actorId,
		IReadOnlyList<IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>> actionDefs) =>
		session.Search(actorId, actionDefs, BattleSearch.TurnVisit);

	public static IEnumerable<SearchFrame<BattleBoard, ActorSession>> SearchCapabilities(
		this Simulation<BattleBoard, ActorSession> session,
		string actorId,
		EType unitType) =>
		session.SearchTurn(actorId, Capabilities.For(unitType));
}
