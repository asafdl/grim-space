using Godot;
using GrimSpace.Ui.Components;
using GrimSpace.Ui.Portraits;

namespace GrimSpace.Presentation.Dev.Hud;

public sealed partial class PilotSpeechHudPreview : Control
{
	private static readonly Dictionary<string, string> PortraitIds = new(StringComparer.Ordinal)
	{
		["lead"] = "pilot_01",
		["wing"] = "pilot_04",
		["robot"] = "pilot_09",
	};
	private PilotSpeechHost _speech = null!;
	private PilotPortrait _hudPortrait = null!;
	private string _focusedPilot = "lead";
	private double _elapsed;

	public override void _Ready()
	{
		UiHudPreviewChrome.Attach(this, "Pilot speech preview",
			"Click: replay · Space: focus wing / lead · Non-focused barks follow moving screen anchors");

		var column = new VBoxContainer { Position = new Vector2(16, 96) };
		column.AddThemeConstantOverride("separation", 38);
		AddChild(column);
		_hudPortrait = new PilotPortrait(PortraitIds[_focusedPilot])
		{
			CustomMinimumSize = new Vector2(64, 64),
		};
		column.AddChild(_hudPortrait);

		_speech = new PilotSpeechHost(ResolveAnchor, layer: 12);
		AddChild(_speech);
		Callable.From(Replay).CallDeferred();
	}

	public override void _Process(double delta) => _elapsed += delta;

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space })
			return;
		_focusedPilot = _focusedPilot == "lead" ? "wing" : "lead";
		_hudPortrait.SetIdentity(PortraitIds[_focusedPilot]);
		GetViewport().SetInputAsHandled();
	}

	private PilotSpeechHost.Anchor? ResolveAnchor(string pilotId)
	{
		if (pilotId == _focusedPilot)
			return PilotSpeechHost.PortraitAnchor(_hudPortrait);
		var viewport = GetViewportRect().Size;
		var position = pilotId == "robot"
			? new Vector2(viewport.X - 24, viewport.Y * .65f)
			: new Vector2(viewport.X * .5f + Mathf.Sin((float)_elapsed) * 80, viewport.Y * .45f);
		return new PilotSpeechHost.Anchor(new Rect2(position, Vector2.Zero),
			Above: true, PortraitId: PortraitIds[pilotId]);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			Replay();
	}

	private void Replay()
	{
		_speech.Clear();
		_speech.Say("wing", "Contacts on the scope.");
		_speech.Say("robot", "Probability of profit: low.\nProbability of explosions: high.");
		_speech.Say("lead", "Oh, Syndi's beard, WHAT. A. RUSTBOX. this system is, how did I even end up here?");
		_speech.Say("lead", "Oh right, the drugs...");
	}
}
