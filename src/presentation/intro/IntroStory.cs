using Godot;

namespace GrimSpace.Presentation.Intro;

[GlobalClass]
public partial class IntroStory : Resource
{
	[Export] public string Title { get; set; } = "";

	[Export] public string[] Pages { get; set; } = [];

	/// <summary>Optional per-page backgrounds; index aligns with <see cref="Pages"/>.</summary>
	[Export] public Texture2D[] Backgrounds { get; set; } = [];
}
