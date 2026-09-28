using Godot;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class FacilityCalloutScreenLayoutTests
{
	[Fact]
	public void SeparateIconCenters_SpreadsParallelVerticalStacksHorizontally()
	{
		var centers = new Vector2[]
		{
			new(400f, 300f),
			new(402f, 330f),
		};

		var resolved = FacilityCalloutScreenLayout.SeparateIconCenters(
			centers,
			FacilityFacadeCallout.IconSize);

		var deltaX = Mathf.Abs(resolved[1].X - resolved[0].X);
		Assert.True(deltaX >= FacilityFacadeCallout.IconSize.X + FacilityCalloutScreenLayout.DefaultIconGap - 0.01f);
	}
}
