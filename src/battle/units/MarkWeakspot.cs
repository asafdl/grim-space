using GrimSpace.Battle.Statuses;

namespace GrimSpace.Battle.Units;

public static class MarkWeakspot
{
	public static bool IsActive(State unit) => unit.Has<MarkWeakspotDebuff>();

	public static MarkWeakspotDebuff? TryStatus(State unit) => unit.StatusOf<MarkWeakspotDebuff>();
}
