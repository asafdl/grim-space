using Godot;
using GrimSpace.Ui.Components;
using GrimSpace.World.StarSystem.Presentation.Ui;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class PilotSpeechHudPreview : Control
{
	private readonly Dictionary<string, Control> _portraits = new(StringComparer.Ordinal);
	private PilotSpeechHost _speech = null!;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(this, "Pilot speech preview", "Click to replay barks");

		var column = new VBoxContainer { Position = new Vector2(16, 96) };
		column.AddThemeConstantOverride("separation", 38);
		AddChild(column);
		foreach (var (pilotId, portraitId) in new[] { ("lead", "pilot_01"), ("wing", "pilot_04"), ("robot", "pilot_09") })
		{
			var portrait = new TextureRect
			{
				Texture = ShipPilotPortraitCatalog.TextureFor(portraitId),
				CustomMinimumSize = new Vector2(64, 64),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
			};
			column.AddChild(portrait);
			_portraits[pilotId] = portrait;
		}

		_speech = new PilotSpeechHost(
			pilotId => _portraits.GetValueOrDefault(pilotId),
			layer: 12);
		AddChild(_speech);
		Callable.From(Replay).CallDeferred();
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			Replay();
	}

	private void Replay()
	{
		_speech.Clear();
		_speech.Say("lead", "Oh, Syndi's beard, WHAT. A. RUSTBOX. this system is, how did I even end up here?");
		_speech.Say("lead", "Oh right, the drugs...");
		_speech.Say("wing", "Contacts on the scope.");
		_speech.Say("robot", "Probability of profit: low.\nProbability of explosions: high.");
	}
}
