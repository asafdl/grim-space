using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Flat navigational void — not a battle chamber. No fog, nebulae, or starfield.
/// </summary>
public sealed partial class MapBackdrop : Node3D
{
	public void Build()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.02f, 0.05f), // ~#02050D
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.18f, 0.24f, 0.34f),
				AmbientLightEnergy = 0.12f,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				FogEnabled = false,
			},
		});
	}
}
