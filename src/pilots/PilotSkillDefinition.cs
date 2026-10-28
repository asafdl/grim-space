using GrimSpace.Components;

namespace GrimSpace.Pilots;

public sealed class PilotSkillDefinition
{
	public EPilotSkillId Id { get; }
	public IReadOnlyList<EPilotCapabilityId> CapabilityIds { get; }
	public HudTooltipContent Tooltip { get; }
	public string Name => Tooltip.Title!;
	public string Description => Tooltip.Body;
	public int Tier { get; }
	public IReadOnlyList<EPilotSkillId> Prerequisites { get; }
	public int MinimumEarnedLevel { get; }
	public PilotSkillAudience Audience { get; }

	public PilotSkillDefinition(
		EPilotSkillId id,
		IReadOnlyList<EPilotCapabilityId> capabilityIds,
		HudTooltipContent tooltip,
		int tier,
		IReadOnlyList<EPilotSkillId>? prerequisites = null,
		int minimumEarnedLevel = 1,
		PilotSkillAudience audience = default)
	{
		ArgumentNullException.ThrowIfNull(tooltip);
		if (string.IsNullOrWhiteSpace(tooltip.Title))
			throw new ArgumentException("A pilot skill tooltip must have a title.", nameof(tooltip));
		if (string.IsNullOrWhiteSpace(tooltip.Body))
			throw new ArgumentException("A pilot skill tooltip must have a body.", nameof(tooltip));
		ArgumentNullException.ThrowIfNull(capabilityIds);
		if (capabilityIds.Count == 0)
			throw new ArgumentException("A pilot skill must grant at least one capability.", nameof(capabilityIds));
		ArgumentOutOfRangeException.ThrowIfLessThan(tier, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(minimumEarnedLevel, 1);

		Id = id;
		CapabilityIds = capabilityIds;
		Audience = audience.Kind == default ? PilotSkillAudience.Global : audience;
		Audience.Validate();
		Tooltip = WithOriginLine(tooltip, Audience);
		Tier = tier;
		Prerequisites = prerequisites ?? Array.Empty<EPilotSkillId>();
		MinimumEarnedLevel = minimumEarnedLevel;
	}

	private static HudTooltipContent WithOriginLine(
		HudTooltipContent tooltip,
		PilotSkillAudience audience)
	{
		var origin = $"Origin: {audience.OriginLabel()}";
		var details = string.IsNullOrWhiteSpace(tooltip.Details)
			? origin
			: $"{origin}\n{tooltip.Details}";
		return tooltip with { Details = details };
	}
}
