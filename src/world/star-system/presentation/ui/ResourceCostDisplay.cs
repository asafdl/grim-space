using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public static class ResourceCostDisplay
{
	private const int IconSize = 16;
	private const int EconomyIconSize = 20;
	private const int EconomyBadgeSize = 10;

	public static Control CreateAmountWithIcon(ResourceId id, int amount, HudTextRole role = HudTextRole.Metadata)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddThemeConstantOverride("separation", 4);

		var amountLabel = new Label
		{
			Text = amount.ToString(),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(amountLabel, role);
		row.AddChild(amountLabel);
		row.AddChild(CreateIcon(id));
		return row;
	}

	public static Control CreateAmountWithBadgeIcon(
		int amount,
		string iconPath,
		string tooltip,
		HudTextRole role = HudTextRole.Metadata)
	{
		ArgumentException.ThrowIfNullOrEmpty(iconPath);
		ArgumentException.ThrowIfNullOrEmpty(tooltip);

		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddThemeConstantOverride("separation", 4);

		var amountLabel = new Label
		{
			Text = amount.ToString(),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(amountLabel, role);
		row.AddChild(amountLabel);
		row.AddChild(CreateBadgedIcon(iconPath, tooltip));
		return row;
	}

	public static Control CreateBundleInline(ResourceBundle bundle, HudTextRole role = HudTextRole.Metadata)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddThemeConstantOverride("separation", 8);

		var added = false;
		foreach (var (id, amount) in bundle.OrderBy(entry => entry.Key))
		{
			if (added)
				row.AddChild(CreateSeparatorDot(role));

			row.AddChild(CreateAmountWithIcon(id, amount, role));
			added = true;
		}

		if (!added)
		{
			var free = new Label { Text = "free", MouseFilter = Control.MouseFilterEnum.Ignore };
			HudStyles.ApplyTextRole(free, role);
			row.AddChild(free);
		}

		return row;
	}

	public static Control CreateOfferDetails(string description, ResourceBundle cost)
	{
		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		row.AddThemeConstantOverride("separation", 24);
		row.AddChild(new Label
		{
			Text = description,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "MerchantOfferStatLabel",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		});
		row.AddChild(CreateBundleInline(cost));
		return row;
	}

	public static Control CreatePrefixed(string prefix, ResourceBundle bundle, HudTextRole role = HudTextRole.Metadata)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddThemeConstantOverride("separation", 8);

		var prefixLabel = new Label
		{
			Text = prefix,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(prefixLabel, role);
		row.AddChild(prefixLabel);
		row.AddChild(CreateBundleInline(bundle, role));
		return row;
	}

	public static Control CreateMetadataRow(ResourceBundle cost, string suffix, HudTextRole role = HudTextRole.Metadata)
	{
		var row = new HBoxContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(CreatePrefixed("COST", cost, role));

		if (!string.IsNullOrEmpty(suffix))
		{
			row.AddChild(CreateSeparatorDot(role));
			var suffixLabel = new Label
			{
				Text = suffix,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				VerticalAlignment = VerticalAlignment.Center,
			};
			HudStyles.ApplyTextRole(suffixLabel, role);
			row.AddChild(suffixLabel);
		}

		return row;
	}

	public static Button CreateLabeledCostButton(
		string label,
		ResourceId resourceId,
		int amount,
		bool enabled,
		Action onPressed)
	{
		var button = new Button
		{
			Text = "",
			Disabled = !enabled,
			CustomMinimumSize = new Vector2(0, 44),
		};
		HudStyles.StyleButton(button, HudActionKind.Primary);

		var content = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		content.AddThemeConstantOverride("separation", 6);

		var labelNode = new Label
		{
			Text = label,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		content.AddChild(labelNode);

		if (enabled && amount > 0)
		{
			content.AddChild(CreateSeparatorDot(HudTextRole.Metadata));
			content.AddChild(CreateAmountWithIcon(resourceId, amount));
		}

		button.AddChild(content);
		if (enabled)
			button.Pressed += onPressed;

		return button;
	}

	private static Control CreateSeparatorDot(HudTextRole role)
	{
		var dot = new Label
		{
			Text = "·",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(dot, role);
		return dot;
	}

	private static TextureRect CreateIcon(ResourceId id) =>
		new()
		{
			Texture = GD.Load<Texture2D>(ResourceIconCatalog.IconPath(id)),
			CustomMinimumSize = new Vector2(IconSize, IconSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			TooltipText = ResourceIconCatalog.DisplayName(id),
		};

	private static Control CreateBadgedIcon(string iconPath, string tooltip)
	{
		var composite = new Control
		{
			CustomMinimumSize = new Vector2(EconomyIconSize, EconomyIconSize),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			TooltipText = tooltip,
		};
		var symbol = new TextureRect
		{
			Texture = GD.Load<Texture2D>(iconPath)
				?? throw new InvalidOperationException($"Could not load economy icon '{iconPath}'."),
			CustomMinimumSize = new Vector2(EconomyIconSize, EconomyIconSize),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Size = new Vector2(EconomyIconSize, EconomyIconSize),
		};
		composite.AddChild(symbol);

		var creditBadge = CreateIcon(ResourceId.Credits);
		creditBadge.CustomMinimumSize = new Vector2(EconomyBadgeSize, EconomyBadgeSize);
		creditBadge.Size = new Vector2(EconomyBadgeSize, EconomyBadgeSize);
		creditBadge.Position = new Vector2(
			EconomyIconSize - EconomyBadgeSize,
			EconomyIconSize - EconomyBadgeSize);
		composite.AddChild(creditBadge);
		return composite;
	}
}
