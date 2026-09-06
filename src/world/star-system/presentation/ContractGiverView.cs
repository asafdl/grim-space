using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class ContractGiverView : TextureButton
{
	public event Action? GiverClicked;

	public override void _Ready()
	{
		Pressed += () => GiverClicked?.Invoke();
		MouseEntered += () => Modulate = new Color(1.08f, 1.08f, 1.08f);
		MouseExited += () => Modulate = Colors.White;
	}
}
