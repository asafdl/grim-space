using Godot;
using GrimSpace.Application;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class BattleVictoryUiSfx
{
	private const string LevelUpSlamPath = "res://assets/sfx/ui/pilot_level_up_slam.wav";

	private static AudioStream? _levelUpSlamStream;

	public static void PlayLevelUpSlam(Node host)
	{
		var player = new AudioStreamPlayer
		{
			Stream = LevelUpSlamStream(),
			Bus = AudioBuses.Sfx,
		};
		host.AddChild(player);
		player.Play();
		player.Finished += player.QueueFree;
	}

	private static AudioStream LevelUpSlamStream() =>
		_levelUpSlamStream ??= GD.Load<AudioStream>(LevelUpSlamPath)
			?? throw new InvalidOperationException($"Could not load SFX '{LevelUpSlamPath}'.");
}
