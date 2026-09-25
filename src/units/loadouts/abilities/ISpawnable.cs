using GrimSpace.Units.Specs;

namespace GrimSpace.Units.Loadouts.Abilities;

public interface ISpawnable
{
	ShipSpec ChildSpec { get; }
	int MaxLivingChildren { get; }
}
