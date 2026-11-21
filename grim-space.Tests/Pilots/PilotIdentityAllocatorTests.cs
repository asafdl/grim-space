using GrimSpace.Pilots;
using GrimSpace.Run;

namespace GrimSpace.Tests.Pilots;

public sealed class PilotIdentityAllocatorTests
{
	[Fact]
	public void Roll_DoesNotReuseNameOrNicknameWithinRun()
	{
		var allocator = new PilotIdentityAllocator();
		var rng = new Random(7);
		var names = new HashSet<string>(StringComparer.Ordinal);
		var nicknames = new HashSet<string>(StringComparer.Ordinal);

		for (var i = 0; i < 20; i++)
		{
			var (name, nickname) = allocator.Roll(
				EPilotBackgroundId.PodRacer,
				[EPilotQuirkId.TriggerHappy],
				rng);
			Assert.True(names.Add(name), $"duplicate name '{name}'");
			Assert.True(nicknames.Add(nickname), $"duplicate nickname '{nickname}'");
		}
	}

	[Fact]
	public void Roll_PrefersTaggedNicknamesWhenAvailable()
	{
		var allocator = new PilotIdentityAllocator();
		var hits = 0;
		for (var seed = 0; seed < 40; seed++)
		{
			var local = new PilotIdentityAllocator();
			var (_, nickname) = local.Roll(
				EPilotBackgroundId.SynthSinger,
				[EPilotQuirkId.Charismatic],
				new Random(seed));
			var entry = PilotNicknameCatalog.All.First(e => e.Nickname == nickname
				|| nickname.StartsWith(e.Nickname + "-", StringComparison.Ordinal));
			if (entry.BackgroundTags.Contains(EPilotBackgroundId.SynthSinger)
				|| entry.QuirkTags.Contains(EPilotQuirkId.Charismatic))
				hits++;
		}

		Assert.True(hits >= 20, $"expected tagged nicknames often, got {hits}/40");
	}

	[Fact]
	public void Suggest_AvoidsUsedWithoutClaiming()
	{
		var allocator = new PilotIdentityAllocator();
		allocator.Claim("EFFY", "Sparks");
		var batchNames = new HashSet<string>(StringComparer.Ordinal);
		var batchNicks = new HashSet<string>(StringComparer.Ordinal);
		var (name, nickname) = allocator.Suggest(
			EPilotBackgroundId.ScrapDrone,
			[EPilotQuirkId.ICanFixThat],
			new Random(3),
			batchNames,
			batchNicks);

		Assert.NotEqual("EFFY", name);
		Assert.NotEqual("Sparks", nickname);
		Assert.Contains("EFFY", allocator.UsedNames);
		Assert.Contains("Sparks", allocator.UsedNicknames);
		Assert.Single(allocator.UsedNames);
		Assert.Single(allocator.UsedNicknames);
		Assert.Contains(name, batchNames);
		Assert.Contains(nickname, batchNicks);
	}

	[Fact]
	public void Reassign_ReleasesOldWhenUnusedElsewhere()
	{
		var allocator = new PilotIdentityAllocator();
		allocator.Claim("Old Name", "OldNick");
		allocator.Reassign(
			"Old Name",
			"OldNick",
			"New Name",
			"NewNick",
			[]);

		Assert.DoesNotContain("Old Name", allocator.UsedNames);
		Assert.DoesNotContain("OldNick", allocator.UsedNicknames);
		Assert.Contains("New Name", allocator.UsedNames);
		Assert.Contains("NewNick", allocator.UsedNicknames);
	}
}
