using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Presentation.Ui;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

internal static class MerchantUiListItem
{
	public static UiListItem Create(
		string title,
		string metadata,
		ResourceBundle cost,
		bool canAfford,
		Action onPressed)
	{
		var item = new UiListItem(
			title,
			metadata,
			ResourceCostDisplay.FormatCompact(cost))
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		item.SetVisual(UiListItemVisual.Technical);
		item.SetAffordable(canAfford);
		item.Pressed += onPressed;
		return item;
	}

	public static UiListItem Create(
		string title,
		string metadata,
		string trailing,
		bool canAfford,
		Action onPressed)
	{
		var item = new UiListItem(title, metadata, trailing)
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		item.SetVisual(UiListItemVisual.Technical);
		item.SetAffordable(canAfford);
		item.Pressed += onPressed;
		return item;
	}
}
