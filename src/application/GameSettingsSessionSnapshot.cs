using Godot;

namespace GrimSpace.Application;

public readonly record struct GameSettingsSessionSnapshot(
	GameSettings.VideoConfig Video,
	GameSettings.AudioConfig Audio,
	bool ShowTutorials,
	bool MouseCameraPan,
	bool ShowActionLog,
	IReadOnlyDictionary<StringName, BindingPair> KeyBindings);
