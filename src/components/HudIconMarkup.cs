using Godot;

namespace GrimSpace.Components;

public static class HudIconMarkup
{
	public static bool Contains(string text) =>
		text.Contains("[icon:", StringComparison.Ordinal);

	public static void Append(
		RichTextLabel label,
		string template,
		Color bodyColor,
		float iconSize,
		bool interactiveIcons = false,
		IReadOnlyDictionary<string, HudTooltipContent>? iconTooltips = null)
	{
		var cursor = 0;
		while (cursor < template.Length)
		{
			var tokenStart = template.IndexOf("[icon:", cursor, StringComparison.Ordinal);
			if (tokenStart < 0)
			{
				AddText(label, template[cursor..], bodyColor);
				break;
			}

			AddText(label, template[cursor..tokenStart], bodyColor);
			var tokenEnd = template.IndexOf(']', tokenStart);
			if (tokenEnd < 0)
				throw new FormatException($"Icon token is missing a closing bracket: {template[tokenStart..]}");

			var token = template[(tokenStart + "[icon:".Length)..tokenEnd].Trim();
			var textSeparator = token.IndexOf(", text:", StringComparison.Ordinal);
			var iconName = (textSeparator >= 0 ? token[..textSeparator] : token).Trim();
			var icon = HudTooltipIconCatalog.Resolve(iconName);
			var texture = GD.Load<Texture2D>(icon.Path)
				?? throw new InvalidOperationException($"Could not load icon '{icon.Path}'.");
			var interactive = interactiveIcons && icon.Tooltip(iconName, iconTooltips) is not null;
			if (interactive)
				label.PushMeta(iconName, RichTextLabel.MetaUnderline.Never, iconName);
			label.AddImage(texture, iconSize, iconSize, icon.TintIcon ? icon.Color : Colors.White);
			if (interactive)
				label.Pop();

			if (textSeparator >= 0)
				AddText(label, token[(textSeparator + ", text:".Length)..].TrimStart(), icon.Color);

			cursor = tokenEnd + 1;
		}
	}

	private static void AddText(RichTextLabel label, string text, Color color)
	{
		if (text.Length == 0)
			return;

		label.PushColor(color);
		label.AddText(text);
		label.Pop();
	}
}
