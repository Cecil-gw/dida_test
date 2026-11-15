using System;
using System.Collections.Generic;

namespace ticktick_WPF.Views.AI;

internal static class AgentNativeMethods
{
	public const string GetStatusBarHeight = "getStatusBarHeight";

	public const string Toast = "toast";

	public const string Track = "track";

	private static readonly HashSet<string> SupportedMethods = new HashSet<string>(new string[3] { "getStatusBarHeight", "toast", "track" }, StringComparer.Ordinal);

	public static bool IsSupported(string methodName)
	{
		if (methodName != null)
		{
			return SupportedMethods.Contains(methodName);
		}
		return false;
	}
}
