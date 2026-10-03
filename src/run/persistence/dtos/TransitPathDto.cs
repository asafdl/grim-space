namespace GrimSpace.Run.Persistence;

internal sealed record TransitPathDto(IReadOnlyList<TransitLegDto> Legs);
