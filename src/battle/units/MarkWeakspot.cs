using GrimSpace.Battle.Statuses;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Units;

public sealed class MarkWeakspotPayload : IBattleStatusPayload
{
	public required ESpatialOrientation Face { get; init; }

	public IBattleStatusPayload DeepCopy() =>
		new MarkWeakspotPayload { Face = Face };
}

public static class MarkWeakspot
{
	public static bool IsActive(State unit) => TryStatus(unit) is not null;

	public static BattleStatus? TryStatus(State unit)
	{
		var instanceId = BattleStatuses.BuffInstanceId("MarkWeakspot", unit.Id);
		return unit.Statuses.Find(status =>
			status.InstanceId == instanceId && status.Payload is MarkWeakspotPayload);
	}

	public static MarkWeakspotPayload? TryPayload(State unit) =>
		TryStatus(unit)?.Payload as MarkWeakspotPayload;
}
