using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation;

public static class FacilityScenes
{
	public const string CommandAuthorityPath = "res://scenes/command_authority.tscn";

	public static string? ResolveScene(Facility facility)
	{
		if (facility.ServiceKinds.Contains(EServiceKind.Contracts))
			return CommandAuthorityPath;

		return null;
	}
}
