using System.Text.Json;

namespace GrimSpace.Run.Persistence;

public sealed record TimelineEntryDto(int Tick, JsonElement Entry);
