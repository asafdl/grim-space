using System.Reflection;

namespace GrimSpace.Application;

public static class GameVersion
{
	private const string ReleaseVersionMetadataKey = "GrimSpaceReleaseVersion";

	public static string Display
	{
		get
		{
			if (TryGetReleaseVersion(out var release))
				return release;

			var informational = Assembly
				.GetExecutingAssembly()
				.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				?.InformationalVersion;
			if (string.IsNullOrEmpty(informational))
				return "dev";

			var plus = informational.IndexOf('+');
			return plus >= 0 ? informational[..plus] : informational;
		}
	}

	public static bool TryGetReleaseVersion(out string version)
	{
		version = typeof(GameVersion).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(a => a.Key == ReleaseVersionMetadataKey)
			?.Value
			?? string.Empty;
		return version.Length > 0;
	}
}
