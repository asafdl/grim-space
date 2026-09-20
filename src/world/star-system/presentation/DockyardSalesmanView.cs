using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class DockyardSalesmanView : TextureButton
{
	public event Action? SalesmanClicked;

	public override void _Ready()
	{
		Pressed += () => SalesmanClicked?.Invoke();
		MouseEntered += () => Modulate = new Color(1.08f, 1.08f, 1.08f);
		MouseExited += () => Modulate = Colors.White;
	}
}
