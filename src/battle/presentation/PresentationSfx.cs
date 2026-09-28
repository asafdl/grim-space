using Godot;
using GrimSpace.Application;

namespace GrimSpace.Battle.Presentation;

public static class PresentationSfx
{
	public const string LightningCannonFirePath =
		"res://assets/sfx/abilities/lightning_cannon_charge_snap.wav";
	public const string LightningCannonMountChargePath =
		"res://assets/sfx/abilities/lightning_cannon_mount_charge.wav";

	private static readonly Dictionary<string, AudioStream> Streams = new();

	public static void PlayWorldOneShot(Node parent, Vector3 localPosition, string path)
	{
		var player = new AudioStreamPlayer3D
		{
			Stream = Load(path),
			Position = localPosition,
			Bus = AudioBuses.Sfx,
			MaxDistance = 4096f,
			AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled,
		};
		parent.AddChild(player);
		player.Play();
		player.Finished += player.QueueFree;
	}

	private static AudioStream Load(string path) =>
		Streams.TryGetValue(path, out var cached)
			? cached
			: Streams[path] = GD.Load<AudioStream>(path)
				?? throw new InvalidOperationException($"Could not load SFX '{path}'.");
}
