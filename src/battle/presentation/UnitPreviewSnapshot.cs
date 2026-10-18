using GrimSpace.Battle.Player;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

public sealed record UnitPreviewSnapshot(
	IReadOnlyDictionary<string, UnitDisplayState> Units,
	IReadOnlyDictionary<string, ETeam> Teams);
