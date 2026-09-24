using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record WreckageObjective(
	string WreckageId,
	AreaPick SearchArea,
	WreckageOutcome Outcome) : IContractObjective
{
	public Coord Position => SearchArea.SpawnPoints[0];
}
