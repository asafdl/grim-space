using Godot;

namespace GrimSpace.Application;

public readonly record struct GameSettingsSessionSnapshot(
	GameSettings.VideoConfig Video,
	GameSettings.AudioConfig Audio,
	bool ShowTutorials,
	bool MouseCameraPan,
	bool InvertHorizontalCameraMovement,
	bool InvertVerticalCameraMovement,
	bool ShowActionLog,
	IReadOnlyDictionary<StringName, BindingPair> KeyBindings);
