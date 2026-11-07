using GrimSpace.Components;

namespace GrimSpace.Pilots;

public sealed class PilotBackgroundDefinition
{
	public EPilotBackgroundId Id { get; }
	public IReadOnlyList<EPilotCapabilityId> CapabilityIds { get; }
	public PilotPersonality PersonalityModifier { get; }
	public HudTooltipContent Tooltip { get; }
	public string Name => Tooltip.Title!;
	public string Description => Tooltip.Body;

	public PilotBackgroundDefinition(
		EPilotBackgroundId id,
		IReadOnlyList<EPilotCapabilityId> capabilityIds,
		HudTooltipContent tooltip,
		PilotPersonality personalityModifier = default)
	{
		ArgumentNullException.ThrowIfNull(capabilityIds);
		ArgumentNullException.ThrowIfNull(tooltip);
		if (string.IsNullOrWhiteSpace(tooltip.Title))
			throw new ArgumentException("A pilot background tooltip must have a title.", nameof(tooltip));
		if (string.IsNullOrWhiteSpace(tooltip.Body))
			throw new ArgumentException("A pilot background tooltip must have a body.", nameof(tooltip));

		Id = id;
		CapabilityIds = capabilityIds;
		PersonalityModifier = personalityModifier;
		Tooltip = tooltip;
	}
}
