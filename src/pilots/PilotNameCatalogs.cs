namespace GrimSpace.Pilots;

public static class RobotNameCatalog
{
	private static readonly string[] Names =
	[
		"EFFY",
		"GiZMo",
		"CL4NK",
		"ZORP",
		"B33P",
		"UNIT-7",
		"SCRAPLET",
		"OVR-DRV",
		"POLY",
		"WRENCH-E",
		"GLIM",
		"K0RTEX",
		"BOLT",
		"NUTZ",
		"GEARZ",
		"PIXL",
		"VLT",
		"SPARK-E",
		"HEX",
		"NOD3",
		"RIVET",
		"FLUX",
		"TINK",
		"CRANK",
		"BITZ",
		"SERVO",
		"CHIP",
		"WELD0",
		"MAGNET",
		"P1STON",
		"COG",
		"RELAY",
		"SOCKET",
		"DR1LL",
		"FUSE",
		"GASKET",
		"TORQ",
		"AXLE",
		"CLAMP",
		"DIODE",
		"KERRL",
		"BLIP",
		"STATIC",
		"HISS",
		"CLANKR",
		"SCRAPZ",
		"UNIT-3",
		"UNIT-9",
		"M0D",
		"PR0XY",
		"N3T",
		"C0R3",
		"B0T",
		"WIR3",
		"JUNKR",
		"RUSTY",
		"P1NG",
		"ZAP",
		"KLICK",
		"THR0B",
		"GR1ND",
		"S0LD1ER",
		"PATCH",
		"TAPE",
		"DUCT",
		"NUB",
		"WHIRR",
		"BZZT",
		"KLUNK",
		"PING0",
	];

	public static IReadOnlyList<string> All { get; } = Names;

	public static string Roll(Random rng, IReadOnlySet<string>? used = null)
	{
		ArgumentNullException.ThrowIfNull(rng);
		used ??= EmptyNames;

		foreach (var index in ShuffledIndices(Names.Length, rng))
		{
			var name = Names[index];
			if (!ContainsIgnoreCase(used, name))
				return name;
		}

		return Names[rng.Next(Names.Length)] + "-" + rng.Next(1000, 9999);
	}

	private static bool ContainsIgnoreCase(IReadOnlySet<string> used, string name)
	{
		if (used.Contains(name))
			return true;
		foreach (var existing in used)
		{
			if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static readonly HashSet<string> EmptyNames = new(StringComparer.OrdinalIgnoreCase);

	private static int[] ShuffledIndices(int count, Random rng)
	{
		var indices = Enumerable.Range(0, count).ToArray();
		for (var i = indices.Length - 1; i > 0; i--)
		{
			var j = rng.Next(i + 1);
			(indices[i], indices[j]) = (indices[j], indices[i]);
		}

		return indices;
	}
}

public sealed record PilotNicknameEntry(
	string Nickname,
	IReadOnlyList<EPilotBackgroundId> BackgroundTags,
	IReadOnlyList<EPilotQuirkId> QuirkTags);

public static class PilotNicknameCatalog
{
	private static readonly PilotNicknameEntry[] Entries =
	[
		new("Sparks", [EPilotBackgroundId.ScrapDrone, EPilotBackgroundId.CyberJunkie], [EPilotQuirkId.ICanFixThat]),
		new("Weld", [EPilotBackgroundId.ScrapDrone, EPilotBackgroundId.HydraulicAbuser], [EPilotQuirkId.MetalMaster, EPilotQuirkId.WeakWelding]),
		new("The Patch", [EPilotBackgroundId.ScrapDrone], [EPilotQuirkId.RoughPatchJob, EPilotQuirkId.ICanFixThat]),
		new("Glitch", [EPilotBackgroundId.CyberJunkie, EPilotBackgroundId.ScrapDrone], [EPilotQuirkId.BadTechnician]),
		new("Static", [EPilotBackgroundId.CyberJunkie, EPilotBackgroundId.SynthSinger], []),
		new("Bolt", [EPilotBackgroundId.HydraulicAbuser, EPilotBackgroundId.GearPsycho], [EPilotQuirkId.TriggerHappy]),
		new("Torque", [EPilotBackgroundId.HydraulicAbuser, EPilotBackgroundId.RetiredMachineManager], []),
		new("Gasket", [EPilotBackgroundId.HydraulicAbuser], [EPilotQuirkId.CloggedVents]),
		new("The Ledger", [EPilotBackgroundId.BeanCounter, EPilotBackgroundId.CorporateSlave], []),
		new("Margin", [EPilotBackgroundId.BeanCounter], []),
		new("Quota", [EPilotBackgroundId.CorporateSlave, EPilotBackgroundId.BeanCounter], []),
		new("Cubicle", [EPilotBackgroundId.CorporateSlave], [EPilotQuirkId.Coward]),
		new("Badge", [EPilotBackgroundId.OptimalityEnforcer], [EPilotQuirkId.PirateHater]),
		new("Protocol", [EPilotBackgroundId.OptimalityEnforcer, EPilotBackgroundId.RetiredMachineManager], []),
		new("Clamp", [EPilotBackgroundId.OptimalityEnforcer], [EPilotQuirkId.ShieldMaster]),
		new("Drift", [EPilotBackgroundId.AsteroidSurfer, EPilotBackgroundId.PodRacer], []),
		new("Wake", [EPilotBackgroundId.AsteroidSurfer], []),
		new("Ski", [EPilotBackgroundId.AsteroidSurfer], [EPilotQuirkId.IceGears]),
		new("Throttle", [EPilotBackgroundId.PodRacer, EPilotBackgroundId.GearPsycho], [EPilotQuirkId.TriggerHappy]),
		new("The Apex", [EPilotBackgroundId.PodRacer], []),
		new("Burn", [EPilotBackgroundId.PodRacer, EPilotBackgroundId.GearPsycho], [EPilotQuirkId.FurnaceCore]),
		new("Chorus", [EPilotBackgroundId.SynthSinger], [EPilotQuirkId.Charismatic]),
		new("Reverb", [EPilotBackgroundId.SynthSinger], []),
		new("Hook", [EPilotBackgroundId.SynthSinger], [EPilotQuirkId.TeamPlayer]),
		new("Shift", [EPilotBackgroundId.RetiredMachineManager], []),
		new("The Foreman", [EPilotBackgroundId.RetiredMachineManager], [EPilotQuirkId.TeamPlayer]),
		new("Clip", [EPilotBackgroundId.RetiredMachineManager, EPilotBackgroundId.OptimalityEnforcer], []),
		new("Jack", [EPilotBackgroundId.CyberJunkie], [EPilotQuirkId.NeuroDrugAddict]),
		new("Spike", [EPilotBackgroundId.CyberJunkie, EPilotBackgroundId.GearPsycho], [EPilotQuirkId.NeuroDrugAddict]),
		new("The Frenzy", [EPilotBackgroundId.GearPsycho], [EPilotQuirkId.FurnaceCore, EPilotQuirkId.TriggerHappy]),
		new("Howl", [EPilotBackgroundId.GearPsycho], []),
		new("Rivet", [], [EPilotQuirkId.MetalMaster]),
		new("The Aegis", [], [EPilotQuirkId.ShieldMaster]),
		new("Radiator", [], [EPilotQuirkId.RadiatorFanatic, EPilotQuirkId.IceGears]),
		new("Frostbite", [], [EPilotQuirkId.IceGears]),
		new("Scorch", [], [EPilotQuirkId.FurnaceCore]),
		new("Lucky", [], [EPilotQuirkId.Charismatic]),
		new("The Ghost", [], [EPilotQuirkId.Coward]),
		new("Scratch", [], [EPilotQuirkId.RoughPatchJob]),
		new("Drip", [], [EPilotQuirkId.CloggedVents]),
		new("The Hammer", [], [EPilotQuirkId.WeakWelding]),
		new("The Fixer", [], [EPilotQuirkId.ICanFixThat]),
		new("Rabid", [], [EPilotQuirkId.PirateHater]),
		new("Muzzle", [], [EPilotQuirkId.TriggerHappy]),
		new("Buddy", [], [EPilotQuirkId.TeamPlayer]),
		new("Dust", [], []),
		new("Echo", [], []),
		new("The Comet", [], []),
		new("Rusty", [], []),
		new("Nickel", [], []),
		new("Proxy", [], []),
		new("Vector", [], []),
		new("Nova", [], []),
		new("Relay", [], []),
		new("The Cipher", [], []),
		new("The Anchor", [], []),
		new("Wisp", [], []),
		new("Cannon", [], []),
		new("Sling", [], []),
		new("Zero", [], []),
		new("Halo", [], []),
		new("Brick", [], []),
		new("Fizz", [], []),
		new("Kite", [], []),
	];

	public static IReadOnlyList<PilotNicknameEntry> All { get; } = Entries;

	public static string Roll(
		EPilotBackgroundId backgroundId,
		IReadOnlyList<EPilotQuirkId> quirkIds,
		Random rng,
		IReadOnlySet<string>? used = null)
	{
		ArgumentNullException.ThrowIfNull(quirkIds);
		ArgumentNullException.ThrowIfNull(rng);
		used ??= EmptyNicknames;

		var preferred = Entries.Where(entry =>
			!used.Contains(entry.Nickname) && Matches(entry, backgroundId, quirkIds)).ToArray();
		if (preferred.Length > 0)
			return preferred[rng.Next(preferred.Length)].Nickname;

		var fallback = Entries.Where(entry => !used.Contains(entry.Nickname)).ToArray();
		if (fallback.Length > 0)
			return fallback[rng.Next(fallback.Length)].Nickname;

		return Entries[rng.Next(Entries.Length)].Nickname + "-" + rng.Next(1000, 9999);
	}

	private static bool Matches(
		PilotNicknameEntry entry,
		EPilotBackgroundId backgroundId,
		IReadOnlyList<EPilotQuirkId> quirkIds)
	{
		if (entry.BackgroundTags.Contains(backgroundId))
			return true;
		for (var i = 0; i < quirkIds.Count; i++)
		{
			if (entry.QuirkTags.Contains(quirkIds[i]))
				return true;
		}

		return false;
	}

	private static readonly HashSet<string> EmptyNicknames = new(StringComparer.Ordinal);
}
