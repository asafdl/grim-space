using System.Text.Json;
using GrimSpace.Units;

namespace GrimSpace.Run.Persistence;

public sealed record StarMapFleetDto(
	JsonElement State,
	IReadOnlyList<string> Members,
	IReadOnlyList<ShipSpawnDeclaration> Registrations);
