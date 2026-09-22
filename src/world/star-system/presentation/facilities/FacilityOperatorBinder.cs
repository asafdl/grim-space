using Godot;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public static class FacilityOperatorBinder
{
	public static void Bind(
		Node operatorRoot,
		PointOfInterest poi,
		Facility facility,
		Action<FacilityOperator, EFacilityOperatorRole> onActivated)
	{
		foreach (var facilityOperator in facility.Operators)
		{
			var button = operatorRoot.GetNodeOrNull<FacilityOperatorButtonView>(facilityOperator.SceneSlotId);
			if (button is null)
				throw new InvalidOperationException(
					$"Facility '{facility.Id}' expects scene slot '{facilityOperator.SceneSlotId}' " +
					$"to be a {nameof(FacilityOperatorButtonView)} under '{operatorRoot.Name}'.");

			button.TooltipText = OperatorDisplayLabels.Title(facilityOperator);
			var captured = facilityOperator;
			button.Pressed += () =>
			{
				var role = poi.ResolveInteractionRole(facility.Id, captured.Name, captured.Role);
				onActivated(captured, role);
			};
		}
	}
}
