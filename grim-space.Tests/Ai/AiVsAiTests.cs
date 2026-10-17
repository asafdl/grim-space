using Xunit;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Units;

namespace grim_space.Tests.Ai;

public class AiVsAiTests
{
	[Fact]
	public async Task AiVsAi_RunsToCompletion()
	{
		var encounter = AiVsAiRunner.CreateAiVsAiEncounter(seed: 12345);
		var result = await AiVsAiRunner.RunAsync(encounter, maxTurns: 50, onTurnLog: (turn, msg) =>
		{
			Console.WriteLine($"[Turn {turn}] {msg}");
		});

		Assert.NotEqual(EBattleResult.Ongoing, result.Winner);
		Assert.True(result.Turns > 0 && result.Turns <= 50);
		Console.WriteLine(result.ToString());
	}

	[Fact]
	public async Task AiVsAi_MultipleSeeds_AllComplete()
	{
		var seeds = new[] { 1, 42, 123, 456, 789 };
		foreach (var seed in seeds)
		{
			var encounter = AiVsAiRunner.CreateAiVsAiEncounter(seed: seed);
			var result = await AiVsAiRunner.RunAsync(encounter, maxTurns: 50);
			Assert.NotEqual(EBattleResult.Ongoing, result.Winner);
			Console.WriteLine($"Seed {seed}: {result}");
		}
	}
}