using Godot;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class SimulationBackdropView : Node3D
{
	private SubViewport _backdropViewport = null!;
	private Camera3D _backdropCamera = null!;

	public ShaderMaterial TintMaterial { get; private set; } = null!;
	public WorldEnvironment WorldEnvironment { get; private set; } = null!;

	public void Build(BoundedGrid grid, int seed, Camera3D camera)
	{
		_backdropViewport = new SubViewport
		{
			Name = "BackdropViewport",
			OwnWorld3D = true,
			Size = (Vector2I)GetViewport().GetVisibleRect().Size,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		AddChild(_backdropViewport);
		_backdropCamera = new Camera3D { Current = true };
		_backdropViewport.AddChild(_backdropCamera);
		SyncCamera(camera);
		var backdrop = new SpaceBackdrop { Name = "SpaceBackdrop" };
		_backdropViewport.AddChild(backdrop);
		backdrop.Build(grid, seed, _backdropCamera);
		var backdropLight = new DirectionalLight3D();
		_backdropViewport.AddChild(backdropLight);
		RedDwarfSun.Configure(
			backdropLight,
			WorldMapping.GridCenter(grid),
			grid.Width * WorldMapping.CellSize * 0.5f);

		TintMaterial = SimulationModePresentation.CreateBackdropTintMaterial();
		var layer = new CanvasLayer { Name = "BackdropLayer", Layer = -100 };
		AddChild(layer);
		layer.AddChild(new TextureRect
		{
			Texture = _backdropViewport.GetTexture(),
			Material = TintMaterial,
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});

		WorldEnvironment = SpaceBackdrop.CreateWorldEnvironment();
		WorldEnvironment.Environment.BackgroundMode = Godot.Environment.BGMode.Canvas;
		WorldEnvironment.Environment.BackgroundCanvasMaxLayer = -100;
		AddChild(WorldEnvironment);
	}

	public void SyncCamera(Camera3D camera)
	{
		var size = (Vector2I)GetViewport().GetVisibleRect().Size;
		if (_backdropViewport.Size != size)
			_backdropViewport.Size = size;
		_backdropCamera.GlobalTransform = camera.GlobalTransform;
		_backdropCamera.Projection = camera.Projection;
		_backdropCamera.Fov = camera.Fov;
		_backdropCamera.Size = camera.Size;
		_backdropCamera.Near = camera.Near;
		_backdropCamera.Far = camera.Far;
		_backdropCamera.KeepAspect = camera.KeepAspect;
	}
}
