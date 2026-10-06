namespace GrimSpace.World.StarSystem.Contact;

public abstract record ContactTarget;

public sealed record FleetContactTarget(string UnitId) : ContactTarget;

public sealed record WreckContactTarget(string ContractId) : ContactTarget;

public enum EContactIntent
{
	Engagement,
	DeliveryMeeting,
	WreckInvestigation,
}

public sealed record ContactReached(
	string ActorId,
	ContactTarget Target,
	EContactIntent? Intent);
