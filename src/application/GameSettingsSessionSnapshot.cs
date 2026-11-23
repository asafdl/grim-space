using Godot;

namespace GrimSpace.Application;

public readonly record struct GameSettingsSessionSnapshot(
	GameSettings.VideoConfig Video,
	GameSettings.AudioConfig Audio,
	bool ShowTutorials,
	bool BattleMouseCameraPan,
	bool StarMapMouseCameraPan,
	bool InvertHorizontalCameraMovement,
	bool InvertVerticalCameraMovement,
	float BattleCameraSensitivity,
	bool ShowActionLog,
	bool BattleCameraRecenterAfterMovement,
	IReadOnlyDictionary<StringName, BindingPair> KeyBindings);
