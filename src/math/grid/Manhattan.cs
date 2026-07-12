using System.Collections.Generic;

namespace GrimSpace.Math.Grid;

public static class Manhattan
{
	public static int L1Norm(int x, int y, int z) =>
		System.Math.Abs(x) + System.Math.Abs(y) + System.Math.Abs(z);

	public static IEnumerable<(int Forward, int Right, int Up)> EnumerateShell(int radius)
	{
		for (var localForward = -radius; localForward <= radius; localForward++)
		{
			for (var localRight = -radius; localRight <= radius; localRight++)
			{
				for (var localUp = -radius; localUp <= radius; localUp++)
				{
					if (L1Norm(localForward, localRight, localUp) == radius)
						yield return (localForward, localRight, localUp);
				}
			}
		}
	}
}
