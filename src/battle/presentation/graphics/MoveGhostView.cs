using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveGhostView : Node3D
{
	private UnitView? _view;
	private EType? _type;
	private Node3D? _guide;
	private Label3D? _unavailableLabel;

	public override void _Ready()
	{
		_guide = new Node3D { Name = "OrientationGuide" };
		AddChild(_guide);

		foreach (var (offset, color) in new[]
		{
			(new Vector3(1f, 0f, 0f), Colors.Red),
			(new Vector3(-1f, 0f, 0f), Colors.DarkRed),
			(new Vector3(0f, 1f, 0f), Colors.Green),
			(new Vector3(0f, -1f, 0f), Colors.DarkGreen),
			(new Vector3(0f, 0f, 1f), Colors.Blue),
			(new Vector3(0f, 0f, -1f), Colors.DarkBlue),
		})
		{
			var marker = new MeshInstance3D
			{
				Position = offset,
				Mesh = new SphereMesh { Radius = 0.08f, Height = 0.16f },
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
				MaterialOverride = new StandardMaterial3D
				{
					ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
					AlbedoColor = color,
				},
			};
			_guide.AddChild(marker);
		}

		_unavailableLabel = new Label3D
		{
			Text = "UNREACHABLE",
			Position = new Vector3(0f, 1.4f, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 32,
			OutlineSize = 6,
			Modulate = new Color(1f, 0.25f, 0.2f),
		};
		_guide.AddChild(_unavailableLabel);
	}

	public void Apply(UnitDisplayState? state, bool available, Color color)
	{
		if (state is null)
		{
			Visible = false;
			return;
		}

		if (_view is null || _type != state.Type)
		{
			_view?.QueueFree();
			_view = new UnitView { Name = "GhostUnit" };
			_view.Bind(state.ToState(), color);
			AddChild(_view);
			_type = state.Type;
		}

		_view.Sync(state.ToState());
		_view.SetGhost(available);
		_guide!.Position = WorldMapping.ToWorld(state.Position);
		_unavailableLabel!.Visible = !available;
		Visible = true;
	}
}
