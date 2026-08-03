using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

/// <summary>
/// Turn-scoped movement preview: prefix-keyed path index for fast lookup as the player queues actions.
/// </summary>
public sealed class MoveUi
{
	private readonly MoveOptionIndex _index;

	private MoveUi(MoveOptionIndex index) => _index = index;

	public static MoveUi Build(BattleSimulation sim, string actorId)
	{
		var searchTimer = Stopwatch.StartNew();
		var index = MoveOptionIndex.FromSimulation(sim, actorId);
		searchTimer.Stop();

		GameLog.Log(
			$"MoveUi.Build ({actorId}): prefixes={index.PrefixCount} "
			+ $"search={searchTimer.Elapsed.TotalMilliseconds:F1}ms");

		return new MoveUi(index);
	}

	public IReadOnlyList<MovePathSession> GetMovePaths(IReadOnlyList<IAction> committed) =>
		_index.GetPaths(committed);

	public bool TryLocate(IReadOnlyList<IAction> committed) =>
		_index.ContainsPrefix(committed);

	internal IEnumerable<IReadOnlyList<IAction>> EnumeratePrefixes() =>
		_index.EnumeratePrefixes();

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
