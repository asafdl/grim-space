using GrimSpace.Pilots;
using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Merchants;

public sealed record PilotHireCommitted(PilotRecruitProfile Profile, ShipSpawnDeclaration Ship);
