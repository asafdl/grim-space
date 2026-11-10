using Godot;
using GrimSpace.Pilots;

namespace GrimSpace.Ui.Portraits;

public sealed partial class PilotPortrait : TextureRect
{
	private readonly SubViewport _viewport;
	private readonly RobotPortrait _robot;
	private string? _portraitId;

	public PilotPortrait() : this(PilotPortraitId.Default)
	{
	}

	public PilotPortrait(string portraitId)
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ExpandMode = ExpandModeEnum.IgnoreSize;
		StretchMode = StretchModeEnum.KeepAspectCovered;
		_viewport = new SubViewport
		{
			Size = new Vector2I(320, 320),
			Disable3D = true,
			TransparentBg = true,
			GuiDisableInput = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
		};
		AddChild(_viewport);
		_robot = new RobotPortrait { Size = new Vector2(320, 320) };
		_robot.EyeOpennessChanged += () =>
			_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		_viewport.AddChild(_robot);
		Texture = _viewport.GetTexture();
		SetIdentity(portraitId);
	}

	public override void _Process(double delta) =>
		_robot.SetProcess(IsVisibleInTree());

	public void SetIdentity(string portraitId)
	{
		if (string.Equals(_portraitId, portraitId, StringComparison.Ordinal))
			return;

		var avatar = RobotPortraitGenerator.Generate(PilotPortraitId.SeedFor(portraitId));
		_robot.Parts = avatar.Parts;
		_robot.Palette = RobotPortraitPalette.FromColors(avatar.Colors);
		_portraitId = portraitId;
		_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
	}
}
