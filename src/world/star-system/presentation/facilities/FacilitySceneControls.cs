using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class FacilitySceneControls
{
	public static UiButton InstallLeaveButton(Node owner, Action onPressed)
	{
		var placeholder = owner.GetNodeOrNull<Node>("Back");
		var position = placeholder is Control control
			? control.Position
			: new Vector2(16f, 16f);
		placeholder?.QueueFree();

		var button = HudWidgets.CreateLeaveButton();
		button.Position = position;
		owner.AddChild(button);
		button.Pressed += onPressed;
		return button;
	}
}
