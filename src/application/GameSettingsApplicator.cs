using Godot;

namespace GrimSpace.Application;

/// <summary>
/// Applies settings to the running game and persists them when the user commits.
/// </summary>
public static class GameSettingsApplicator
{
	public enum Host
	{
		MainMenu,
		InRun,
	}

	public static GameSettingsSessionSnapshot CapturePersisted() =>
		new(
			GameSettings.ReadVideoConfig(),
			GameSettings.ReadAudioConfig(),
			GameSettings.ReadShowTutorials(),
			GameSettings.ReadMouseCameraPan(),
			GameSettings.ReadShowActionLog(),
			GameSettings.ReadKeyBindings());

	public static void Preview(GameSettingsSessionSnapshot snapshot)
	{
		GameSettings.ApplyVideoConfig(snapshot.Video);
		GameSettings.ApplyAudioConfig(snapshot.Audio);
		GameSettings.ApplyMouseCameraPan(snapshot.MouseCameraPan);
		GameSettings.ApplyShowActionLog(snapshot.ShowActionLog);
		GameInputBindings.Apply(snapshot.KeyBindings);
	}

	public static bool TryCommit(
		GameSettingsSessionSnapshot snapshot,
		Host host,
		out string? error)
	{
		if (!GameInputBindings.TryValidateCommitted(snapshot.KeyBindings, out error))
			return false;

		var tutorialsChanged = snapshot.ShowTutorials != GameSettings.ReadShowTutorials();
		if (GameSettings.SaveAll(
				snapshot.Video,
				snapshot.Audio,
				snapshot.ShowTutorials,
				snapshot.MouseCameraPan,
				snapshot.ShowActionLog,
				snapshot.KeyBindings) != Error.Ok)
		{
			error = "Could not save settings to disk.";
			return false;
		}

		Preview(snapshot);
		if (tutorialsChanged && host == Host.MainMenu)
		{
			Session.Instance.DiscardPreparedRun();
			Session.Instance.PrepareFirstScene();
		}

		error = null;
		return true;
	}

	public static void CommitVideo(GameSettings.VideoConfig video)
	{
		GameSettings.SaveVideoConfig(video);
		GameSettings.ApplyVideoConfig(video);
	}

	public static void CommitAudio(GameSettings.AudioConfig audio)
	{
		GameSettings.SaveAudioConfig(audio);
		GameSettings.ApplyAudioConfig(audio);
	}

	public static void CommitGameplayPreferences(
		bool showTutorials,
		bool mouseCameraPan,
		bool showActionLog,
		Host host)
	{
		var tutorialsChanged = showTutorials != GameSettings.ReadShowTutorials();
		if (GameSettings.SaveGameplayPreferences(showTutorials, mouseCameraPan, showActionLog) != Error.Ok)
			return;

		GameSettings.ApplyMouseCameraPan(mouseCameraPan);
		GameSettings.ApplyShowActionLog(showActionLog);
		if (tutorialsChanged && host == Host.MainMenu)
		{
			Session.Instance.DiscardPreparedRun();
			Session.Instance.PrepareFirstScene();
		}
	}

	public static bool TryCommitKeyBindings(
		IReadOnlyDictionary<StringName, BindingPair> bindings,
		out string? error)
	{
		if (!GameInputBindings.TryValidateCommitted(bindings, out error))
			return false;

		GameInputBindings.Apply(bindings);
		var config = GameSettings.LoadOrCreateConfig();
		GameInputBindings.WriteBindingsSection(config, bindings);
		if (config.Save(GameSettings.SettingsFilePath) != Error.Ok)
		{
			error = "Could not save key bindings to disk.";
			return false;
		}

		error = null;
		return true;
	}
}
