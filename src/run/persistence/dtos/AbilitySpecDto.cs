using System.Text.Json;

namespace GrimSpace.Run.Persistence;

public sealed record AbilitySpecDto(string Kind, JsonElement Data);
