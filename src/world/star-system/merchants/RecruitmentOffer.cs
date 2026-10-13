using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public sealed record RecruitmentOffer(EType Chassis, EShipGearTier GearTier, ResourceBundle Cost);
