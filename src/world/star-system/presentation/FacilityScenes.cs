using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation;

public static class FacilityScenes
{
	public const string CommandAuthorityPath = "res://scenes/command_authority.tscn";
	public const string DockyardPath = "res://scenes/dockyard.tscn";

	public static string? ResolveScene(Facility facility)
	{
		if (facility.ServiceKinds.Contains(EServiceKind.Contracts))
			return CommandAuthorityPath;

		if (facility.ServiceKinds.Contains(EServiceKind.Dockyard))
			return DockyardPath;

		return null;
	}
}
