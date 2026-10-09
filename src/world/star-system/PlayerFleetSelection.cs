using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public static class PlayerFleetSelection
{
	public static string? EnsureSelectedMember(
		StarMap map,
		ActorRuntime runtime,
		string fleetId,
		IReadOnlyList<string> partyShipIds)
	{
		if (!map.FleetRegistry.TryGet(fleetId, out var fleet))
		{
			runtime.SelectedMemberShipId = null;
			return null;
		}

		if (partyShipIds.Count == 0)
		{
			runtime.SelectedMemberShipId = null;
			return null;
		}

		var memberIds = fleet.Members.Select(member => member.Id).ToHashSet(StringComparer.Ordinal);
		if (runtime.SelectedMemberShipId is { } current
			&& memberIds.Contains(current)
			&& partyShipIds.Contains(current, StringComparer.Ordinal))
			return current;

		var fallback = partyShipIds.FirstOrDefault(id => memberIds.Contains(id))
			?? memberIds.FirstOrDefault();
		runtime.SelectedMemberShipId = fallback;
		return fallback;
	}
}
