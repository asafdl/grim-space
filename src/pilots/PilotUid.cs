using System.Security.Cryptography;
using System.Text;

namespace GrimSpace.Pilots;

public static class PilotUid
{
	public static string FromId(string pilotId)
	{
		ArgumentException.ThrowIfNullOrEmpty(pilotId);
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(pilotId));
		return Convert.ToHexString(hash.AsSpan(0, 2));
	}
}
