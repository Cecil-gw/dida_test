using System;

namespace ticktick_WPF.Util;

public static class ErrorHelper
{
	public static void VerifySucceeded(uint hresult)
	{
		if (hresult > 1)
		{
			throw new Exception("Failed with HRESULT: " + hresult.ToString("X"));
		}
	}
}
