using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

/// <summary>
/// Turn-scoped movement preview: on-demand path discovery cached by committed action prefix.
/// </summary>
public sealed class MoveUi
{
	private readonly string _playerId;
	private readonly MovePreviewCache _cache;

	private MoveUi(string playerId, MovePreviewCache cache)
	{
		_playerId = playerId;
		_cache = cache;
	}

	public static MoveUi Build(BattleSimulation sim, string playerId) => new(playerId, new MovePreviewCache());

	public IReadOnlyList<MovePathSession> GetMovePaths(
		BattleSimulation sim,
		string actorId,
		IReadOnlyList<IAction> committed) =>
		actorId == _playerId
			? _cache.GetPaths(sim, _playerId, committed)
			: MovePathEndpoints.DiscoverExtensions(sim, actorId);

	public static (IReadOnlyList<Coord> Path, Coord? Target) GetPathHighlights(
		IReadOnlyList<MovePathSession> paths,
		int? hoveredIndex,
		IReadOnlyList<Coord> committedPath)
	{
		if (hoveredIndex is int i)
			return (paths[i].Cells, paths[i].EndPosition);

		if (committedPath.Count > 0)
			return (committedPath, committedPath[^1]);

		return ([], null);
	}
}
