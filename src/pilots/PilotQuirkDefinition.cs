using GrimSpace.Components;

namespace GrimSpace.Pilots;

public sealed class PilotQuirkDefinition
{
	public EPilotQuirkId Id { get; }
	public EPilotQuirkPolarity Polarity { get; }
	public IReadOnlyList<EPilotCapabilityId> CapabilityIds { get; }
	public HudTooltipContent Tooltip { get; }
	public string Name => Tooltip.Title!;
	public string Description => Tooltip.Body;

	public PilotQuirkDefinition(
		EPilotQuirkId id,
		EPilotQuirkPolarity polarity,
		IReadOnlyList<EPilotCapabilityId> capabilityIds,
		HudTooltipContent tooltip)
	{
		ArgumentNullException.ThrowIfNull(capabilityIds);
		ArgumentNullException.ThrowIfNull(tooltip);
		if (string.IsNullOrWhiteSpace(tooltip.Title))
			throw new ArgumentException("A pilot quirk tooltip must have a title.", nameof(tooltip));
		if (string.IsNullOrWhiteSpace(tooltip.Body))
			throw new ArgumentException("A pilot quirk tooltip must have a body.", nameof(tooltip));

		Id = id;
		Polarity = polarity;
		CapabilityIds = capabilityIds;
		Tooltip = tooltip;
	}
}
